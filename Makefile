.PHONY: data-folder 

data-folder:
	if [ ! -d "./Node/data" ]; then mkdir -p ./Node/data; fi

node:
	(cd Node && dotnet publish -c Release -o app)

web:
	(cd Web && dotnet publish -c Release -o app)

run:
	make node
	make web
	make data-folder
	docker compose up --remove-orphans

rolling-upgrade:
	bash rolling-upgrade.sh

# ------------------- Testing ------------------

run-cmdline-node:
	make data-folder
	(cd Node && DATA_FOLDER=./data dotnet run)

run-docker-node:
	make node
	make data-folder
	docker run --rm -p 11111:11111 -p 30000:30000 -v $(PWD)/Node/app:/app -v $(PWD)/Node/data:/data -e DATA_FOLDER=/data --name node --workdir /app --entrypoint dotnet mcr.microsoft.com/dotnet/aspnet:8.0 /app/Node.dll

