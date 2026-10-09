# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY RealEstateCRM/RealEstateCRM.csproj RealEstateCRM/
RUN dotnet restore RealEstateCRM/RealEstateCRM.csproj
COPY RealEstateCRM/ RealEstateCRM/
RUN dotnet publish RealEstateCRM/RealEstateCRM.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .
# Render injects PORT; Program.cs binds to it. 8080 is the fallback for plain `docker run`.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RealEstateCRM.dll"]
