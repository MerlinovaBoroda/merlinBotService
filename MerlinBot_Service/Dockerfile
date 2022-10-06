FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MelrinBot_Service/MelrinBot_Service.csproj", "MelrinBot_Service/"]
RUN dotnet restore "MelrinBot_Service/MelrinBot_Service.csproj"
COPY . .
WORKDIR "/src/MelrinBot_Service"
RUN dotnet build "MelrinBot_Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MelrinBot_Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MelrinBot_Service.dll"]
