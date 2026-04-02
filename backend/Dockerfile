FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ResumeChat.Api/ResumeChat.Api.csproj src/ResumeChat.Api/
COPY src/ResumeChat.Rag/ResumeChat.Rag.csproj src/ResumeChat.Rag/
COPY src/ResumeChat.ServiceDefaults/ResumeChat.ServiceDefaults.csproj src/ResumeChat.ServiceDefaults/
COPY tests/ResumeChat.Api.Tests/ResumeChat.Api.Tests.csproj tests/ResumeChat.Api.Tests/
COPY tests/ResumeChat.Rag.Tests/ResumeChat.Rag.Tests.csproj tests/ResumeChat.Rag.Tests/
RUN dotnet restore src/ResumeChat.Api/ResumeChat.Api.csproj
RUN dotnet restore tests/ResumeChat.Api.Tests/ResumeChat.Api.Tests.csproj
RUN dotnet restore tests/ResumeChat.Rag.Tests/ResumeChat.Rag.Tests.csproj

COPY src/ResumeChat.Api/ src/ResumeChat.Api/
COPY src/ResumeChat.Rag/ src/ResumeChat.Rag/
COPY src/ResumeChat.ServiceDefaults/ src/ResumeChat.ServiceDefaults/
COPY tests/ tests/
RUN dotnet test tests/ResumeChat.Api.Tests/ --no-restore --verbosity quiet
RUN dotnet test tests/ResumeChat.Rag.Tests/ --no-restore --verbosity quiet
RUN dotnet publish src/ResumeChat.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ResumeChat.Api.dll"]
