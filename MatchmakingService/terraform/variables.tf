variable "aws_region" {
  description = "Region de AWS"
  type        = string
  default     = "us-east-1"
}

variable "ecs_cluster_name" {
  description = "Nombre del cluster ECS existente"
  type        = string
  default     = "game-server-d233"
}

variable "listener_arn" {
  description = "ARN del Listener HTTP del ALB"
  type        = string
}

variable "vpc_id" {
  description = "ID de la VPC"
  type        = string
}

variable "subnet_ids" {
  description = "Lista de IDs de subredes publicas"
  type        = list(string)
}

variable "ecr_image_uri" {
  description = "URI completa de la imagen en ECR"
  type        = string
}

variable "game_server_url" {
  description = "URL interna del ALB para comunicarse con GameServer"
  type        = string
}

variable "container_port" {
  description = "Puerto del contenedor"
  type        = number
  default     = 3000
}

variable "max_players" {
  description = "Maximo de jugadores por lobby"
  type        = number
  default     = 4
}

variable "ecs_execution_role_name" {
  description = "Nombre del rol IAM de ejecucion de ECS"
  type        = string
  default     = "LabRole"
}

variable "desired_count" {
  description = "Numero de tareas del servicio"
  type        = number
  default     = 1
}
