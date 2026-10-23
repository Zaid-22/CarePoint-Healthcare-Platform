FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY CarePoint.Domain/CarePoint.Domain.csproj CarePoint.Domain/
COPY CarePoint.Application/CarePoint.Application.csproj CarePoint.Application/
COPY CarePoint.Infrastructure/CarePoint.Infrastructure.csproj CarePoint.Infrastructure/
COPY CarePoint.API/CarePoint.API.csproj CarePoint.API/
COPY CarePoint.Tests/CarePoint.Tests.csproj CarePoint.Tests/
RUN dotnet restore CarePoint.API/CarePoint.API.csproj

# Now copy everything and publish.
COPY . .
RUN dotnet publish CarePoint.API/CarePoint.API.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Run as non-root for security.
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN mkdir -p /app/App_Data/medical-documents /app/App_Data/profile-images \
    && chown -R appuser:appgroup /app/App_Data
USER appuser

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarePoint.API.dll"]
