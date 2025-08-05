# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["NXM.Tensai.Back.OKR.API/NXM.Tensai.Back.OKR.API.csproj", "NXM.Tensai.Back.OKR.API/"]
COPY ["NXM.Tensai.Back.OKR.AI/NXM.Tensai.Back.OKR.AI.csproj", "NXM.Tensai.Back.OKR.AI/"]
COPY ["NXM.Tensai.Back.OKR.Application/NXM.Tensai.Back.OKR.Application.csproj", "NXM.Tensai.Back.OKR.Application/"]
COPY ["NXM.Tensai.Back.OKR.Domain/NXM.Tensai.Back.OKR.Domain.csproj", "NXM.Tensai.Back.OKR.Domain/"]
COPY ["NXM.Tensai.Back.OKR.Infrastructure/NXM.Tensai.Back.OKR.Infrastructure.csproj", "NXM.Tensai.Back.OKR.Infrastructure/"]
RUN dotnet restore "./NXM.Tensai.Back.OKR.API/NXM.Tensai.Back.OKR.API.csproj"
COPY . .
WORKDIR "/src/NXM.Tensai.Back.OKR.API"
RUN dotnet build "NXM.Tensai.Back.OKR.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "NXM.Tensai.Back.OKR.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NXM.Tensai.Back.OKR.API.dll"]