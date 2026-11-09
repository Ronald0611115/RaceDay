
# Stage 1: Build MVC
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY RaceDay.MVC/RaceDay.MVC.csproj RaceDay.MVC/

RUN dotnet restore RaceDay.MVC/RaceDay.MVC.csproj

COPY RaceDay.MVC/ RaceDay.MVC/

WORKDIR /src/RaceDay.MVC

RUN dotnet publish RaceDay.MVC.csproj -c Release -o /app/publish --no-restore

# Stage 2: Run MVC
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
