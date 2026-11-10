# Consome a VPC, as subnets, a NLB interna e a HTTP API já provisionadas por
# oficina-mecanica-infra-k8s. O bucket vem de variável — não hardcoded — pelo mesmo motivo do
# backend parcial em backend.tf.
#
# Contrato assumido (ver README de oficina-mecanica-infra-k8s, seção "Contrato de outputs"):
# oficina-mecanica-infra-k8s exporta vpc_id, private_subnet_ids, app_load_balancer_dns_name,
# api_gateway_id e api_gateway_execution_arn.
#
# O state remoto precisa já existir (infra-k8s aplicado) antes de rodar plan/apply aqui — ver
# README, seção "Ordem de deploy".
data "terraform_remote_state" "infra_k8s" {
  backend = "s3"

  config = {
    bucket = var.tf_state_bucket
    key    = var.infra_k8s_state_key
    region = var.aws_region
  }
}

locals {
  vpc_id                     = data.terraform_remote_state.infra_k8s.outputs.vpc_id
  private_subnet_ids         = data.terraform_remote_state.infra_k8s.outputs.private_subnet_ids
  app_load_balancer_dns_name = data.terraform_remote_state.infra_k8s.outputs.app_load_balancer_dns_name
  api_gateway_id             = data.terraform_remote_state.infra_k8s.outputs.api_gateway_id
  api_gateway_execution_arn  = data.terraform_remote_state.infra_k8s.outputs.api_gateway_execution_arn
}
