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
# Data sources
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
# CloudWatch Log Group
# -------------------------------------------------------------------

resource "aws_cloudwatch_log_group" "matchmaking" {
  name              = "/ecs/matchmaking-service"
  retention_in_days = 14

  tags = { Name = "matchmaking-service-logs" }
}

# -------------------------------------------------------------------
# Security Group
# -------------------------------------------------------------------

resource "aws_security_group" "matchmaking" {
  name        = "matchmaking-service-sg"
  description = "Allow traffic from ALB to matchmaking service container"
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

  tags = { Name = "matchmaking-service-sg" }
}

# -------------------------------------------------------------------
# Target Group
# -------------------------------------------------------------------

resource "aws_lb_target_group" "matchmaking" {
  name        = "matchmaking-service-tg"
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

  tags = { Name = "matchmaking-service-tg" }
}

# -------------------------------------------------------------------
# ALB Listener Rule — /api/matchmaking/*
# -------------------------------------------------------------------

resource "aws_lb_listener_rule" "matchmaking" {
  listener_arn = var.listener_arn
  priority     = 400

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.matchmaking.arn
  }

  condition {
    path_pattern {
      values = ["/api/matchmaking/*"]
    }
  }
}

# -------------------------------------------------------------------
# ECS Task Definition
# -------------------------------------------------------------------

resource "aws_ecs_task_definition" "matchmaking" {
  family                   = "matchmaking-service"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = data.aws_iam_role.ecs_execution.arn
  task_role_arn            = data.aws_iam_role.ecs_execution.arn

  container_definitions = jsonencode([
    {
      name  = "matchmaking-service"
      image = var.ecr_image_uri

      portMappings = [
        {
          containerPort = var.container_port
          protocol      = "tcp"
        }
      ]

      environment = [
        { name = "PORT", value = tostring(var.container_port) },
        { name = "GAME_SERVER_URL", value = var.game_server_url },
        { name = "MAX_PLAYERS_PER_LOBBY", value = tostring(var.max_players) }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.matchmaking.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])

  tags = { Name = "matchmaking-service-task" }
}

# -------------------------------------------------------------------
# ECS Service
# -------------------------------------------------------------------

resource "aws_ecs_service" "matchmaking" {
  name            = "matchmaking-service"
  cluster         = data.aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.matchmaking.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.subnet_ids
    assign_public_ip = true
    security_groups  = [aws_security_group.matchmaking.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.matchmaking.arn
    container_name   = "matchmaking-service"
    container_port   = var.container_port
  }

  depends_on = [aws_lb_listener_rule.matchmaking]

  tags = { Name = "matchmaking-service" }
}
