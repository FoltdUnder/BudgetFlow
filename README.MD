Build api
    docker build -f src/BudgetFlow.Api/Dockerfile -t budgetflow-api .

Run api
    docker run --rm -p 8080:8080 --name budgetflow-api budgetflow-api