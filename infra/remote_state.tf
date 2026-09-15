# Consome a VPC, as subnets, o security group do RDS e a HTTP API já provisionados pelos
# repositórios de infraestrutura. O bucket vem de variável — não hardcoded — pelo mesmo motivo do
# backend parcial em backend.tf.
#
# Contrato assumido (ver README dos respectivos repositórios, seção "Contrato de outputs"):
# - oficina-mecanica-infra-k8s exporta vpc_id, private_subnet_ids, api_gateway_id e
#   api_gateway_execution_arn.
# - oficina-mecanica-infra-db exporta db_security_group_id.
#
# Os states remotos precisam já existir (infra-k8s e infra-db aplicados) antes de rodar plan/apply
# aqui — ver README, seção "Ordem de deploy".
data "terraform_remote_state" "infra_k8s" {
  backend = "s3"

  config = {
    bucket = var.tf_state_bucket
    key    = var.infra_k8s_state_key
    region = var.aws_region
  }
}

data "terraform_remote_state" "infra_db" {
  backend = "s3"

  config = {
    bucket = var.tf_state_bucket
    key    = var.infra_db_state_key
    region = var.aws_region
  }
}

locals {
  vpc_id                    = data.terraform_remote_state.infra_k8s.outputs.vpc_id
  private_subnet_ids        = data.terraform_remote_state.infra_k8s.outputs.private_subnet_ids
  api_gateway_id            = data.terraform_remote_state.infra_k8s.outputs.api_gateway_id
  api_gateway_execution_arn = data.terraform_remote_state.infra_k8s.outputs.api_gateway_execution_arn

  db_security_group_id = data.terraform_remote_state.infra_db.outputs.db_security_group_id
}
