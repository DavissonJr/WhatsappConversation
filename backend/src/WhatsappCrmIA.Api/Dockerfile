FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WhatsappCrmIA.sln", "./"]
COPY ["src/WhatsappCrmIA.Domain/WhatsappCrmIA.Domain.csproj", "src/WhatsappCrmIA.Domain/"]
COPY ["src/WhatsappCrmIA.Application/WhatsappCrmIA.Application.csproj", "src/WhatsappCrmIA.Application/"]
COPY ["src/WhatsappCrmIA.Infrastructure/WhatsappCrmIA.Infrastructure.csproj", "src/WhatsappCrmIA.Infrastructure/"]
COPY ["src/WhatsappCrmIA.Api/WhatsappCrmIA.Api.csproj", "src/WhatsappCrmIA.Api/"]
RUN dotnet restore "src/WhatsappCrmIA.Api/WhatsappCrmIA.Api.csproj"

COPY . .
WORKDIR /src/src/WhatsappCrmIA.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WhatsappCrmIA.Api.dll"]
