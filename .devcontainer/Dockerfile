# Devcontainer Dockerfile for ClubDoorman
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Установить дополнительные утилиты
RUN apt-get update && apt-get install -y git curl

WORKDIR /workspace

# Копировать всё содержимое репозитория
COPY . .

# По умолчанию запускать bash
CMD ["/bin/bash"]
