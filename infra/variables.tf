variable "aws_region" {
  description = "Região AWS. A conta AWS Academy Learner Lab só permite us-east-1 e us-west-2 (RFC-002)."
  type        = string
  default     = "us-east-1"
}

variable "tf_state_bucket" {
  description = <<-EOT
    Bucket S3 onde estão os states remotos de oficina-mecanica-infra-k8s e
    oficina-mecanica-infra-db (mesmo bucket compartilhado dos três repositórios de
    infraestrutura). Sem default de propósito: não hardcodar nome de bucket real no código.
  EOT
  type        = string
}

variable "infra_k8s_state_key" {
  description = "Key do state remoto de oficina-mecanica-infra-k8s dentro do bucket compartilhado."
  type        = string
  default     = "infra-k8s/terraform.tfstate"
}

variable "infra_db_state_key" {
  description = "Key do state remoto de oficina-mecanica-infra-db dentro do bucket compartilhado."
  type        = string
  default     = "infra-db/terraform.tfstate"
}

variable "lab_role_name" {
  description = <<-EOT
    Nome da IAM role pré-criada na conta AWS Academy usada pela Lambda (RFC-002 §6.1). Não é
    possível criar IAM roles nesta conta — a única role "Lab*" disponível é `LabRole` (mesma usada
    pelo cluster/node group do EKS em oficina-mecanica-infra-k8s).
  EOT
  type        = string
  default     = "LabRole"
}

variable "function_name" {
  description = "Nome da function no Lambda."
  type        = string
  default     = "oficina-mecanica-lambda-auth"
}

variable "lambda_zip_path" {
  description = <<-EOT
    Caminho local do zip de deploy publicado pela pipeline (dotnet publish + zip, job `package` de
    .github/workflows/ci.yml). Sem default de propósito: informado pela pipeline via -var,
    apontando para o artefato baixado/gerado naquele job.
  EOT
  type        = string
}
