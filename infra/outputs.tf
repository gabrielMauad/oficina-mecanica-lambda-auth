output "lambda_function_name" {
  description = "Nome da function no Lambda."
  value       = aws_lambda_function.auth.function_name
}

output "lambda_function_arn" {
  description = "ARN da function no Lambda."
  value       = aws_lambda_function.auth.arn
}

output "lambda_security_group_id" {
  description = "Security group da Lambda dentro da VPC."
  value       = aws_security_group.lambda.id
}

output "auth_route" {
  description = "Rota criada nesta HTTP API para autenticação por CPF (path relativo, não a URL completa)."
  value       = "POST /auth/cpf"
}
