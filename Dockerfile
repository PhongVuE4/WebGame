# Sử dụng image .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy toàn bộ solution (bao gồm tất cả các project)
COPY . .

# Restore dependencies cho project chính (TruthOrDare_API)
RUN dotnet restore TruthOrDare_API/TruthOrDare_API.csproj

# Build và publish project chính
RUN dotnet publish TruthOrDare_API/TruthOrDare_API.csproj -c Release -o out

# Sử dụng image runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Thiết lập biến môi trường cho ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# Lệnh khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "TruthOrDare_API.dll"]