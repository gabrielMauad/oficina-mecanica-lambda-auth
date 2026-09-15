# Integra esta Lambda na HTTP API já criada por oficina-mecanica-infra-k8s (id em
# api_gateway_id, ver remote_state.tf). Este repositório NÃO cria a API — só acrescenta sua
# própria rota nela.

resource "aws_apigatewayv2_integration" "auth" {
  api_id           = local.api_gateway_id
  integration_type = "AWS_PROXY"
  integration_uri  = aws_lambda_function.auth.invoke_arn
  # 2.0, nao o default 1.0: o handler espera APIGatewayHttpApiV2ProxyRequest (Function.cs).
  payload_format_version = "2.0"
  # Integracoes AWS_PROXY sempre invocam a Lambda via POST, independente do metodo HTTP real do
  # cliente - e assim que a integracao Lambda proxy funciona no API Gateway.
  integration_method = "POST"
}

# Rota mais especifica que "ANY /{proxy+}" (infra-k8s/apigateway.tf) - o API Gateway despacha para
# a integracao mais especifica primeiro, entao esta rota tem precedencia sobre o catch-all da NLB.
resource "aws_apigatewayv2_route" "auth" {
  api_id    = local.api_gateway_id
  route_key = "POST /auth/cpf"
  target    = "integrations/${aws_apigatewayv2_integration.auth.id}"
}

resource "aws_lambda_permission" "apigateway" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.auth.function_name
  principal     = "apigateway.amazonaws.com"
  # /*/* libera qualquer stage e qualquer rota desta API - simples e suficiente aqui (uma unica
  # rota usa esta Lambda); restringir a "/*/POST/auth/cpf" seria mais estrito mas acopla o nome do
  # stage por engano se o formato mudar.
  source_arn = "${local.api_gateway_execution_arn}/*/*"
}
