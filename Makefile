.PHONY: data empty-data

data:
	if [ ! -d "./Node/data" ]; then mkdir -p ./Node/data; fi

empty-data:
	rm -rf ./Node/data

node:
	(cd Node && dotnet publish -c Release -o app)

web:
	(cd Web && dotnet publish -c Release -o app)

run:
	make node
	make web
	make data
	docker compose up --remove-orphans

rolling-upgrade:
	bash rolling-upgrade.sh

# ------------------- Testing ------------------

clean-data:
	make empty-data

reset-data:
	make empty-data

run-single-node:
	make node
	make data
	docker compose up --remove-orphans mongodb node1

run-cmdline-node:
	make data
	(cd Node && DATA_FOLDER=./data dotnet run)

run-docker-node:
	make node
	make data
	docker run --rm -p 11111:11111 -p 30000:30000 -v $(PWD)/Node/app:/app -v $(PWD)/Node/data:/data -e DATA_FOLDER=/data --name node --workdir /app --entrypoint dotnet mcr.microsoft.com/dotnet/aspnet:8.0 /app/Node.dll
