terraform {
  required_version = ">= 1.3"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# -------------------------------------------------------------------
# Data sources – recursos existentes en AWS
# -------------------------------------------------------------------

data "aws_ecs_cluster" "this" {
  cluster_name = var.ecs_cluster_name
}

data "aws_vpc" "this" {
  id = var.vpc_id
}

data "aws_iam_role" "ecs_execution" {
  name = var.ecs_execution_role_name
}

# -------------------------------------------------------------------
# CloudWatch Log Group – logs del contenedor
# -------------------------------------------------------------------

resource "aws_cloudwatch_log_group" "auth" {
  name              = "/ecs/auth-service-60a1"
  retention_in_days = 14

  tags = { Name = "auth-service-60a1-logs" }
}

# -------------------------------------------------------------------
# Security Group – trafico entrante desde el ALB
# -------------------------------------------------------------------

resource "aws_security_group" "auth" {
  name        = "auth-service-60a1-sg"
  description = "Allow traffic from ALB to auth service container"
  vpc_id      = data.aws_vpc.this.id

  ingress {
    description = "Traffic from ALB"
    from_port   = var.container_port
    to_port     = var.container_port
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "All outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "auth-service-60a1-sg" }
}

# -------------------------------------------------------------------
# Target Group – balanceo hacia las tareas del auth service
# -------------------------------------------------------------------

resource "aws_lb_target_group" "auth" {
  name        = "auth-service-60a1-tg"
  port        = var.container_port
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = data.aws_vpc.this.id

  health_check {
    enabled             = true
    path                = "/health"
    port                = "traffic-port"
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    matcher             = "200"
  }

  tags = { Name = "auth-service-60a1-tg" }
}

# -------------------------------------------------------------------
# ALB Listener Rule – /api/auth/* → Target Group
# -------------------------------------------------------------------

resource "aws_lb_listener_rule" "auth" {
  listener_arn = var.listener_arn
  priority     = 100

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.auth.arn
  }

  condition {
    path_pattern {
      values = ["/api/auth/*"]
    }
  }
}

# -------------------------------------------------------------------
# ECS Task Definition – Fargate, 1 vCPU / 2 GB RAM
# -------------------------------------------------------------------

resource "aws_ecs_task_definition" "auth" {
  family                   = "auth-service-60a1"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory
  execution_role_arn       = data.aws_iam_role.ecs_execution.arn
  task_role_arn            = data.aws_iam_role.ecs_execution.arn

  container_definitions = jsonencode([
    {
      name  = "auth-service"
      image = var.ecr_image_uri

      portMappings = [
        {
          containerPort = var.container_port
          protocol      = "tcp"
        }
      ]

      environment = [
        { name = "PORT", value = tostring(var.container_port) },
        { name = "MONGODB_URI", value = var.mongodb_uri }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.auth.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])

  tags = { Name = "auth-service-60a1-task" }
}

# -------------------------------------------------------------------
# ECS Service – Fargate con IP publica
# -------------------------------------------------------------------

resource "aws_ecs_service" "auth" {
  name            = "auth-service-60a1"
  cluster         = data.aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.auth.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.subnet_ids
    assign_public_ip = true
    security_groups  = [aws_security_group.auth.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.auth.arn
    container_name   = "auth-service"
    container_port   = var.container_port
  }

  depends_on = [aws_lb_listener_rule.auth]

  tags = { Name = "auth-service-60a1" }
}
