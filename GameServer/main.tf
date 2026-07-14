provider "aws" {
  region = "us-east-1"
}

locals {
  app_port = 3000
  vpc_id   = "vpc-00ec11dca0610ee1c" # Tu VPC actual de AWS Academy
}

resource "aws_cloudwatch_log_group" "ecs_logs" {
  name              = "/ecs/game-server"
  retention_in_days = 7
}

# -----------------------------------------------------------
# Target Group
# -----------------------------------------------------------
resource "aws_lb_target_group" "game" {
  name        = "game-server-tg"
  port        = local.app_port
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = local.vpc_id

  health_check {
    enabled             = true
    path                = "/health"
    port                = "traffic-port"
    interval            = 30
    unhealthy_threshold = 2
    protocol            = "HTTP"
    matcher             = "200"
  }

  tags = { Name = "game-server-tg" }
}

# -----------------------------------------------------------
# ALB Listener
# -----------------------------------------------------------
resource "aws_lb_listener" "game_http" {
  load_balancer_arn = "arn:aws:elasticloadbalancing:us-east-1:042076316445:loadbalancer/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be"
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.game.arn
  }
}

# -----------------------------------------------------------
# Data source del ALB existente
# -----------------------------------------------------------
data "aws_lb" "existing_alb" {
  arn = "arn:aws:elasticloadbalancing:us-east-1:042076316445:loadbalancer/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be"
}

# -----------------------------------------------------------
# Egress rule del ALB hacia el contenedor en puerto 3000
# -----------------------------------------------------------
resource "aws_vpc_security_group_egress_rule" "alb_to_ecs" {
  description              = "Permitir health check y trafico del ALB al ECS en puerto 3000"
  security_group_id        = tolist(data.aws_lb.existing_alb.security_groups)[0]
  referenced_security_group_id = aws_security_group.ecs_sg.id
  from_port                = local.app_port
  to_port                  = local.app_port
  ip_protocol              = "tcp"
}

# -----------------------------------------------------------
# IAM Role para ECS Execution
# -----------------------------------------------------------
data "aws_iam_role" "ecs_execution" {
  name = "LabRole" 
}

# -----------------------------------------------------------
# ECS Cluster
# -----------------------------------------------------------
resource "aws_ecs_cluster" "game_cluster" {
  name = "game-server-d233"
  tags = { Name = "game-server-d233" }
}

# -----------------------------------------------------------
# Security Group para el Contenedor (ECS)
# -----------------------------------------------------------
resource "aws_security_group" "ecs_sg" {
  name        = "game-server-ecs-sg"
  description = "Permite trafico en el puerto 3000 desde el ALB"
  vpc_id      = local.vpc_id

  ingress {
    from_port   = local.app_port
    to_port     = local.app_port
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] 
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "game-server-ecs-sg" }
}

# -----------------------------------------------------------
# Task Definition
# -----------------------------------------------------------
resource "aws_ecs_task_definition" "game" {
  family                   = "game-server"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = data.aws_iam_role.ecs_execution.arn

  container_definitions = jsonencode([
    {
      name      = "game-server"
      image     = "042076316445.dkr.ecr.us-east-1.amazonaws.com/game-server:latest"
      essential = true
      portMappings = [
        {
          containerPort = local.app_port
          hostPort      = local.app_port
          protocol      = "tcp"
        }
      ]
      environment = [
        {
          name  = "PORT"
          value = tostring(local.app_port)
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/game-server"
          "awslogs-region"        = "us-east-1"
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])

  tags = { Name = "game-server" }
}

# -----------------------------------------------------------
# ECS Service
# -----------------------------------------------------------
resource "aws_ecs_service" "game" {
  name            = "game-server-d233"
  cluster         = aws_ecs_cluster.game_cluster.id
  task_definition = aws_ecs_task_definition.game.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  health_check_grace_period_seconds = 60

  network_configuration {
    subnets          = ["subnet-03b5e829969f8fea7", "subnet-0271cfa9c9e590320"]
    security_groups  = [aws_security_group.ecs_sg.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.game.arn
    container_name   = "game-server"
    container_port   = local.app_port
  }

  depends_on = [
    aws_lb_listener.game_http,
  ]

  tags = { Name = "game-server-d233" }
}