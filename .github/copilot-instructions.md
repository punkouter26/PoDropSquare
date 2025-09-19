# Coding Rules for Blazor WebAssembly / .NET (Check PRD.MD for app description / Check STEPS.MD for 10 high level steps to complete the app)


## 1. Guiding Philosophy
- Automate with CLI tools (dotnet, az, gh, git, azd)  
- Refer to STEPS.MD to know what is the next part of the app to do / Mark step as complete when it is done / if STEPS.MD does not exist then ignore this rule
- Enforce SOLID principles, GoF patterns, Test-Driven Development, and proactive refactoring  
- Maintained Simplicity: Keep the codebase clean, concise, and easy to understand
- Always have the api project run at port 5000 http 5001 https


## 2. Architecture & Maintenance
- Organize by vertical slices and apply Clean Architecture boundaries within each slice or if application code is simple use simple services
- Adhere to Single Responsibility: refactor files that exceed ~500 lines into focused components  
- Remove unused code and files regularly as part of maintenance  
- Make sure the api project is hosting the blazor wasm project on port 5000 http 5001 https / CORS is not needed


## 3. Project Layout
- Root folders:  
  - `/src` for application code  
  - `/tests` for unit, integration, and functional tests  
  - `/docs` for architecture diagrams and documentation  
  - `/scripts` for automation utilities  
- Prefix project names with your company/app code (e.g., `Po.AppName.Api`)  


## 4. API & Observability
- Enable Swagger/OpenAPI documentation from project inception  
- Implement global error-handling middleware using Serilog and RFC 7807 Problem Details  
- Expose a `/api/health` endpoint for readiness and liveness checks  
- Ensure API methods return raw exception messages to callers for easier debugging


## 5. Frontend Components
- Start with built-in Blazor components; adopt Radzen.Blazor for advanced scenarios 


## 6. Data Persistence
- Default to Azure Table Storage with the Azurite emulator for local development  
- Use Azure SQL or Cosmos DB only with tech-lead approval for relational or complex scenarios  
- Name tables using the pattern `PoAppName[TableName]` (e.g., `PoAppNameProducts`)  


## 7. Testing/Running
- Always run the API project when I ask to run app / the API hosts the front end
- Use xUnit for all tests  
- Follow the TDD cycle:  
  Write a failing test /  Implement code to pass the test  /  Refactor while keeping tests green  /Build the UI against the verified API /  Submit feature for review once tests pass  
- Maintain separate unit, integration (with emulated infrastructure), and functional tests  




