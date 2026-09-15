# Configuração parcial: propositalmente sem bucket nem região aqui — nenhum nome de recurso de
# conta fica commitado. Os valores reais entram via `-backend-config` no `terraform init` (ver
# README, seção "Instruções de execução"), tanto localmente quanto no workflow de CI. Mesmo padrão
# de oficina-mecanica-infra-k8s e oficina-mecanica-infra-db — chave própria no mesmo bucket
# compartilhado.
terraform {
  backend "s3" {}
}
