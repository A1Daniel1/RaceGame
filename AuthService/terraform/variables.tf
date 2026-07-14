variable "aws_region" {
  description = "Region de AWS donde se desplegara el servicio"
  type        = string
  default     = "us-east-1"
}

variable "ecs_cluster_name" {
  description = "Nombre del cluster ECS existente"
  type        = string
  default     = "default"
}

variable "listener_arn" {
  description = "ARN del Listener HTTP del ALB (puerto 80) donde se aplicara la regla de reenvio para /api/auth/*"
  type        = string
}

variable "vpc_id" {
  description = "ID de la VPC existente donde se desplegara el servicio"
  type        = string
}

variable "subnet_ids" {
  description = "Lista de IDs de subredes publicas existentes para las tareas Fargate"
  type        = list(string)
}

variable "ecr_image_uri" {
  description = "URI completa de la imagen en ECR (ej: <account>.dkr.ecr.<region>.amazonaws.com/auth-service:latest)"
  type        = string
}

variable "mongodb_uri" {
  description = "URI de conexion a MongoDB (valor sensible, usa una variable de Terraform o un secreto de AWS)"
  type        = string
  sensitive   = true
}

variable "container_port" {
  description = "Puerto del contenedor (la app Express escucha en este puerto via la variable PORT)"
  type        = number
  default     = 3000
}

variable "cpu" {
  description = "CPU para la tarea Fargate (1024 = 1 vCPU)"
  type        = number
  default     = 1024
}

variable "memory" {
  description = "Memoria para la tarea Fargate (2048 = 2 GB)"
  type        = number
  default     = 2048
}

variable "ecs_execution_role_name" {
  description = "Nombre del rol IAM de ejecucion de ECS (en AWS Academy suele ser 'LabRole')"
  type        = string
  default     = "LabRole"
}

variable "desired_count" {
  description = "Numero deseado de tareas del servicio"
  type        = number
  default     = 1
}
