# A Lambda precisa estar na VPC para alcançar a NLB interna que expõe o serviço de Cadastro (a NLB
# não é pública). Consequência aceita: sem NAT Gateway nesta conta (RFC-002), a Lambda na VPC NÃO
# tem acesso à internet nem às APIs da AWS — por isso o segredo de runtime entra como variável de
# ambiente resolvida pelo Terraform no apply (lambda.tf), não buscado em runtime.

resource "aws_security_group" "lambda" {
  name        = "${var.function_name}-sg"
  description = "SG da Function de autenticacao por CPF, na VPC para alcancar a NLB interna do Cadastro"
  vpc_id      = local.vpc_id

  tags = {
    Name = "${var.function_name}-sg"
  }
}

resource "aws_vpc_security_group_egress_rule" "lambda_all" {
  security_group_id = aws_security_group.lambda.id
  description       = "Saida liberada - a Lambda so alcanca a NLB interna do Cadastro (30082) e nada mais critico depende de egress restrito aqui"
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}
