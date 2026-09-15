# A Lambda na VPC desta conta não tem acesso à internet nem às APIs da AWS (sem NAT Gateway,
# RFC-002) — por isso ela NÃO pode buscar segredo no Secrets Manager em tempo de execução. Os
# valores entram como variáveis de ambiente RESOLVIDAS PELO TERRAFORM NO APPLY, lidas aqui via
# data "aws_secretsmanager_secret_version" dos secrets de nome estável
# "oficina-mecanica/dev/rds/postgresql" (oficina-mecanica-infra-db) e "oficina-mecanica/dev/app"
# (oficina-mecanica-infra-k8s). Consequência aceita e documentada no README: esses valores ficam
# na configuração da function e no state deste Terraform — é a única forma possível nesta conta,
# dado que não há caminho de rede da Lambda até o Secrets Manager.
data "aws_secretsmanager_secret_version" "rds" {
  secret_id = "oficina-mecanica/dev/rds/postgresql"
}

data "aws_secretsmanager_secret_version" "app" {
  secret_id = "oficina-mecanica/dev/app"
}

locals {
  rds_secret = jsondecode(data.aws_secretsmanager_secret_version.rds.secret_string)
  app_secret = jsondecode(data.aws_secretsmanager_secret_version.app.secret_string)

  # RDS PostgreSQL 16 exige TLS por padrao (rds.force_ssl = 1) - sem "SSL Mode=Require" a conexao
  # e recusada. Sintaxe de connection string do Npgsql (mesma lib usada em ClienteRepository.cs).
  #
  # ADR-002 (repositorio oficina-mecanica-app) prevê um usuário de banco somente-leitura dedicado
  # para a Lambda. Criá-lo exige executar SQL dentro da VPC (o RDS não é público), inviável a
  # partir do runner do GitHub nesta etapa - ver README, "Limitações conhecidas", e a nota de
  # execução acrescentada ao ADR-002. Por isso a Lambda usa as MESMAS credenciais do secret do
  # RDS que a aplicação usa, não um usuário dedicado.
  db_connection_string = "Host=${local.rds_secret.host};Port=${local.rds_secret.port};Database=${local.rds_secret.dbname};Username=${local.rds_secret.username};Password=${local.rds_secret.password};SSL Mode=Require"
}

# Nenhum aws_iam_role neste repositório - a conta AWS Academy não permite criar IAM roles
# (RFC-002 §6.1). A role pré-criada LabRole é referenciada via data source, mesmo padrão de
# oficina-mecanica-infra-k8s.
data "aws_iam_role" "lab" {
  name = var.lab_role_name
}

resource "aws_lambda_function" "auth" {
  function_name = var.function_name
  runtime       = "dotnet8"
  # Handler EXATO de aws-lambda-tools-defaults.json (src/OficinaMecanica.LambdaAuth/) - divergir
  # aqui faz toda invocacao falhar com "class not found", sem erro nenhum em plan/validate.
  handler = "OficinaMecanica.LambdaAuth::OficinaMecanica.LambdaAuth.Function::FunctionHandler"
  role    = data.aws_iam_role.lab.arn

  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  # 512/15s, nao o default de 256MB/10s de aws-lambda-tools-defaults.json: VPC + conexao ao RDS +
  # cold start do runtime dotnet8 juntos deixam 10s apertado.
  memory_size = 512
  timeout     = 15

  vpc_config {
    subnet_ids         = local.private_subnet_ids
    security_group_ids = [aws_security_group.lambda.id]
  }

  environment {
    variables = {
      DB_CONNECTION_STRING = local.db_connection_string
      JWT_SECRET           = local.app_secret.jwt_secret
      # RFC-001 §4.1: contrato de claims do token do cliente. Valores fixos, nao dinamicos - nao e
      # a mesma categoria de "endereco que muda a cada recriacao" que a REGRA CENTRAL proibe.
      JWT_ISSUER   = "oficina-mecanica-auth"
      JWT_AUDIENCE = "oficina-mecanica-api"
    }
  }
}
