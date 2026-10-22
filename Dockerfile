FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ProcessosJudiciais.Api/ProcessosJudiciais.Api.csproj ProcessosJudiciais.Api/
RUN dotnet restore ProcessosJudiciais.Api/ProcessosJudiciais.Api.csproj

COPY ProcessosJudiciais.Api/ ProcessosJudiciais.Api/
RUN dotnet publish ProcessosJudiciais.Api/ProcessosJudiciais.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProcessosJudiciais.Api.dll"]
