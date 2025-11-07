# Orleans Virtual Actor Rolling Upgrade Sample

Also a good starting point for Orleans

## Demo

Terminal 1:
```
make run
```

Browser
- http://localhost:5000
- get the grid
- select fields
- activate auto-refresh

Change the business logic to show a changed implementation
- in Logic/Field.cs
- change _color value 

Terminal 2:
```
make node
make rolling-upgrade
```

Also Swagger: http://localhost:5000/swagger
