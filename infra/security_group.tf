# A Lambda precisa estar na VPC para alcançar o RDS (ADR-002 da aplicação — o RDS não é público).
# Consequência aceita: sem NAT Gateway nesta conta (RFC-002), a Lambda na VPC NÃO tem acesso à
# internet nem às APIs da AWS — por isso os segredos de runtime entram como variáveis de ambiente
# resolvidas pelo Terraform no apply (lambda.tf), não buscados em runtime.

resource "aws_security_group" "lambda" {
  name        = "${var.function_name}-sg"
  description = "SG da Function de autenticacao por CPF, na VPC para alcancar o RDS"
  vpc_id      = local.vpc_id

  tags = {
    Name = "${var.function_name}-sg"
  }
}

resource "aws_vpc_security_group_egress_rule" "lambda_all" {
  security_group_id = aws_security_group.lambda.id
  description       = "Saida liberada - Lambda em subnet publica sem NAT, so alcanca RDS (5432) e nada mais critico depende de egress restrito aqui"
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}

# A regra que libera esta Lambda no security group do RDS vive AQUI, não em
# oficina-mecanica-infra-db — é este repositório que cria o security group da Lambda, então é
# aqui que a regra nasce (ver README, "Ordem de deploy", e o PR que removeu a variável
# condicional lambda_security_group_id de oficina-mecanica-infra-db).
resource "aws_security_group_rule" "postgres_from_lambda" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  security_group_id        = local.db_security_group_id
  source_security_group_id = aws_security_group.lambda.id
  description              = "Function oficina-mecanica-lambda-auth"
}
