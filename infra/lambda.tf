# A Lambda na VPC desta conta não tem acesso à internet nem às APIs da AWS (sem NAT Gateway,
# RFC-002) — por isso ela NÃO pode buscar segredo no Secrets Manager em tempo de execução. O
# segredo JWT entra como variável de ambiente RESOLVIDA PELO TERRAFORM NO APPLY, lida aqui via
# data "aws_secretsmanager_secret_version" do secret de nome estável "oficina-mecanica/dev/app"
# (oficina-mecanica-infra-k8s). Consequência aceita e documentada no README: o valor fica na
# configuração da function e no state deste Terraform — é a única forma possível nesta conta,
# dado que não há caminho de rede da Lambda até o Secrets Manager.
#
# A Lambda não acessa banco nenhum: consulta o serviço de Cadastro por HTTP (CADASTRO_BASE_URL),
# pela NLB interna, porque nenhum serviço acessa o banco de outro.
data "aws_secretsmanager_secret_version" "app" {
  secret_id = "oficina-mecanica/dev/app"
}

locals {
  app_secret = jsondecode(data.aws_secretsmanager_secret_version.app.secret_string)

  # O listener da NLB para o Cadastro escuta na porta do seu NodePort (30082).
  cadastro_base_url = "http://${local.app_load_balancer_dns_name}:30082"
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

  # 512/15s, nao o default de 256MB/10s de aws-lambda-tools-defaults.json: VPC + chamada HTTP ao
  # Cadastro + cold start do runtime dotnet8 juntos deixam 10s apertado.
  memory_size = 512
  timeout     = 15

  vpc_config {
    subnet_ids         = local.private_subnet_ids
    security_group_ids = [aws_security_group.lambda.id]
  }

  environment {
    variables = {
      CADASTRO_BASE_URL = local.cadastro_base_url
      JWT_SECRET        = local.app_secret.jwt_secret
      # RFC-001 §4.1: contrato de claims do token do cliente. Valores fixos, nao dinamicos - nao e
      # a mesma categoria de "endereco que muda a cada recriacao" que a REGRA CENTRAL proibe.
      JWT_ISSUER   = "oficina-mecanica-auth"
      JWT_AUDIENCE = "oficina-mecanica-api"
    }
  }
}
