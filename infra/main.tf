# Infraestrutura da Function de autenticação por CPF — Fase 3 do Tech Challenge. Provisiona a
# própria Lambda, o security group que ela usa para alcançar o RDS (o RDS não é público) e a
# integração/rota no API Gateway já criado por oficina-mecanica-infra-k8s. Ver README para o
# desenho completo e RFC-002/ADR-006 (repositório oficina-mecanica-app) para as restrições da
# conta AWS Academy Learner Lab que moldam este código.
#
# Os recursos estão organizados por arquivo:
#   backend.tf       - configuração parcial do backend remoto (S3), chave lambda-auth/terraform.tfstate
#   providers.tf      - provider AWS
#   variables.tf      - inputs
#   remote_state.tf   - terraform_remote_state de infra-k8s e infra-db (VPC, subnets, SGs, API Gateway)
#   security_group.tf - security group da Lambda e a regra de ingress 5432 no SG do RDS
#   lambda.tf         - a aws_lambda_function, lendo os segredos de runtime do Secrets Manager
#   apigateway.tf      - integração AWS_PROXY, rota POST /auth/cpf e a permissão de invocação
#   outputs.tf         - outputs informativos deste repositório

terraform {
  required_version = ">= 1.5"
}
