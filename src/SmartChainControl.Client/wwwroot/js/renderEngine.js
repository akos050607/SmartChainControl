window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {}, 
    shadowGenerator: null,
    mirrorTexture: null, 
    
    zoneColors: ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"],

    init: function (canvasId) {
        // Memória ürítése
        this.robotMeshes = {}; 
        this.highlightedShelves = [];

        this.canvas = document.getElementById(canvasId);
        
        this.engine = new BABYLON.Engine(this.canvas, true, { 
            preserveDrawingBuffer: true, 
            antialias: true 
        });
        
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color3(0.02, 0.02, 0.04); 

        var camera = new BABYLON.ArcRotateCamera("Camera", -Math.PI / 2, Math.PI / 3, 24, new BABYLON.Vector3(10, 0, 10), this.scene);
        camera.attachControl(this.canvas, true);
        camera.wheelPrecision = 50;
        camera.minZ = 0.5;

        // Fények
        var hemiLight = new BABYLON.HemisphericLight("hemiLight", new BABYLON.Vector3(0, 1, 0), this.scene);
        hemiLight.intensity = 0.4; 

        var dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-1, -2, -1), this.scene);
        dirLight.position = new BABYLON.Vector3(20, 40, 20);
        dirLight.intensity = 0.8;

        // Árnyék
        this.shadowGenerator = new BABYLON.ShadowGenerator(1024, dirLight);
        this.shadowGenerator.useBlurExponentialShadowMap = true;
        this.shadowGenerator.blurKernel = 16; 

        // Tükröződés
        var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 50, height: 50 }, this.scene);
        ground.position.x = 10;
        ground.position.z = 10;
        ground.receiveShadows = true;

        this.mirrorTexture = new BABYLON.MirrorTexture("mirror", 512, this.scene, true);
        this.mirrorTexture.mirrorPlane = new BABYLON.Plane(0, -1, 0, 0);
        this.mirrorTexture.level = 0.4;

        var groundMat = new BABYLON.StandardMaterial("groundMat", this.scene);
        groundMat.diffuseColor = new BABYLON.Color3(0.05, 0.05, 0.05); 
        groundMat.reflectionTexture = this.mirrorTexture; 
        groundMat.specularColor = new BABYLON.Color3(0.2, 0.2, 0.2); 
        ground.material = groundMat;

        this.createDropOffZones();

        this.engine.runRenderLoop(() => {
            if (this.scene && this.scene.activeCamera) {
                this.scene.render();
            }
        });

        window.addEventListener("resize", () => {
            this.engine.resize();
        });
    },

    createDropOffZones: function() {
        for (let i = 0; i < 5; i++) {
            let colorHex = this.zoneColors[i % this.zoneColors.length];
            let color = BABYLON.Color3.FromHexString(colorHex);
            let yPos = 2 + (i * 3);
            
            var zone = BABYLON.MeshBuilder.CreateGround("zone_" + i, { width: 1.4, height: 1.4 }, this.scene);
            zone.position.x = 0; zone.position.z = yPos; zone.position.y = 0.02;
            
            var zoneMat = new BABYLON.StandardMaterial("zoneMat_" + i, this.scene);
            zoneMat.diffuseColor = color; 
            zoneMat.emissiveColor = color.scale(0.3); 
            zoneMat.alpha = 0.8; 
            zone.material = zoneMat;
            
            var border = BABYLON.MeshBuilder.CreateTorus("zoneBorder_" + i, { diameter: 1.4, thickness: 0.05, tessellation: 32 }, this.scene);
            border.position.x = 0; border.position.z = yPos; border.position.y = 0.03;
            
            var borderMat = new BABYLON.StandardMaterial("borderMat_" + i, this.scene);
            borderMat.emissiveColor = color; 
            borderMat.diffuseColor = new BABYLON.Color3(0,0,0);
            border.material = borderMat;
            
            this.mirrorTexture.renderList.push(border);
        }
    },

    createMap: function (mapData) {
        // Törlés duplikáció ellen
        mapData.obstacles.forEach(obs => {
            var existing = this.scene.getMeshByName("obs_" + obs.x + "_" + obs.y);
            if (existing) existing.dispose();
        });

        var shelfMat = new BABYLON.StandardMaterial("shelfMat", this.scene);
        shelfMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.2); 
        shelfMat.specularColor = new BABYLON.Color3(0.3, 0.3, 0.3); 
        shelfMat.emissiveColor = new BABYLON.Color3(0, 0, 0); 
        
        // JAVÍTÁS: KIVETTÜK A 'shelfMat.freeze()' PARANCSOT!
        // Ez okozta, hogy a polc nem váltott vissza feketére.

        mapData.obstacles.forEach(obs => {
            var shelf = BABYLON.MeshBuilder.CreateBox("obs_" + obs.x + "_" + obs.y, { 
                width: 0.9, depth: 0.9, height: 2.2 
            }, this.scene);

            shelf.position.x = obs.x;
            shelf.position.z = obs.y;
            shelf.position.y = 1.1;
            
            shelf.material = shelfMat;
            shelf.receiveShadows = true;

            this.shadowGenerator.addShadowCaster(shelf);
            this.mirrorTexture.renderList.push(shelf);
        });
    },

    createRobotMesh: function(id, colorHex) {
        var color = BABYLON.Color3.FromHexString(colorHex);
        
        var body = BABYLON.MeshBuilder.CreateBox("body_" + id, { width: 0.8, depth: 0.8, height: 0.25 }, this.scene);
        body.position.y = 0.15; 
        
        var bodyMat = new BABYLON.StandardMaterial("bodyMat_" + id, this.scene);
        bodyMat.diffuseColor = new BABYLON.Color3(0.1, 0.1, 0.1); 
        body.material = bodyMat;

        var core = BABYLON.MeshBuilder.CreateCylinder("core_" + id, { diameter: 0.5, height: 0.05 }, this.scene);
        core.position.y = 0.13; 
        core.parent = body;
        
        var coreMat = new BABYLON.StandardMaterial("coreMat_" + id, this.scene);
        coreMat.emissiveColor = color.scale(1.5); 
        coreMat.diffuseColor = new BABYLON.Color3(0,0,0);
        core.material = coreMat;

        return { mesh: body };
    },

    updateRobots: function (robotsData) {
        // Először lekapcsolunk minden polc-fényt
        this.clearShelfHighlights();

        robotsData.forEach(robot => {
            var robotObj = this.robotMeshes[robot.id];

            if (!robotObj) {
                robotObj = this.createRobotMesh(robot.id, robot.colorHex);

                this.shadowGenerator.addShadowCaster(robotObj.mesh); 
                this.mirrorTexture.renderList.push(robotObj.mesh);   

                var cargoBox = BABYLON.MeshBuilder.CreateBox("cargo_" + robot.id, { size: 0.5 }, this.scene);
                cargoBox.parent = robotObj.mesh;
                cargoBox.position.y = 0.4; 
                
                var cargoMat = new BABYLON.StandardMaterial("cargoMat", this.scene);
                cargoMat.diffuseColor = new BABYLON.Color3(0.9, 0.9, 0.9); 
                cargoBox.material = cargoMat;
                
                robotObj.cargoMesh = cargoBox;
                this.shadowGenerator.addShadowCaster(cargoBox); 
                this.mirrorTexture.renderList.push(cargoBox);

                this.robotMeshes[robot.id] = robotObj;
            }

            var mesh = robotObj.mesh;
            mesh.position.x = BABYLON.Scalar.Lerp(mesh.position.x, robot.x, 0.2);
            mesh.position.z = BABYLON.Scalar.Lerp(mesh.position.z, robot.y, 0.2);
            mesh.position.y = 0.15 + Math.sin(Date.now() * 0.01 + robot.id) * 0.005;

            if (robotObj.cargoMesh) {
                robotObj.cargoMesh.isVisible = robot.hasCargo;
            }

            // Ha van cél, felkapcsoljuk a fényt
            if (robot.currentTargetNode) {
                this.highlightShelf(robot.currentTargetNode.x, robot.currentTargetNode.y, robot.colorHex);
            }
        });
    },

    highlightShelf: function(x, y, colorHex) {
        var shelfId = "obs_" + x + "_" + y;
        var shelfMesh = this.scene.getMeshByName(shelfId);
        
        if (shelfMesh) {
            // Ha kell, klónozzuk az anyagot, hogy egyedileg színezhető legyen
            if (shelfMesh.material.name === "shelfMat") {
                shelfMesh.material = shelfMesh.material.clone("highlightMat_" + x + "_" + y);
            }
            var color = BABYLON.Color3.FromHexString(colorHex);
            shelfMesh.material.emissiveColor = color.scale(0.8);
            
            // Hozzáadjuk a listához, hogy a következő körben le tudjuk kapcsolni
            this.highlightedShelves.push(shelfMesh);
        }
    },

    clearShelfHighlights: function() {
        // Végigmegyünk az előző körben bekapcsolt polcokon és leoltjuk őket
        this.highlightedShelves.forEach(mesh => {
            mesh.material.emissiveColor = new BABYLON.Color3(0, 0, 0); // Fekete = Kikapcsolva
        });
        // Lista ürítése
        this.highlightedShelves = [];
    }
};