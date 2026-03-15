Build api
    docker build -f src/BudgetFlow.Api/Dockerfile -t budgetflow-api .

Run api
    docker run --rm -p 8080:8080 --name budgetflow-api budgetflow-api


Add migrations
    dotnet ef migrations add {migration_name} --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api

update db
    dotnet ef database update --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api

drop db
    dotnet ef database drop --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api

