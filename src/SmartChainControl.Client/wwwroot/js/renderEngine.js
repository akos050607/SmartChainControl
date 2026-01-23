window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {},
    highlightedShelves: [],
    glowLayer: null,
    
    zoneColors: ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"],

    init: function (canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.engine = new BABYLON.Engine(this.canvas, true, { 
            preserveDrawingBuffer: true, stencil: true, adaptToDeviceRatio: true, antialias: true 
        });
        
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color3(0.02, 0.02, 0.04);

        var camera = new BABYLON.ArcRotateCamera("Camera", -Math.PI / 2, Math.PI / 3, 28, new BABYLON.Vector3(10, 0, 10), this.scene);
        camera.attachControl(this.canvas, true);
        camera.wheelPrecision = 50;

        var hemiLight = new BABYLON.HemisphericLight("hemiLight", new BABYLON.Vector3(0, 1, 0), this.scene);
        hemiLight.intensity = 0.5;

        var dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-1, -2, -1), this.scene);
        dirLight.position = new BABYLON.Vector3(20, 40, 20);
        dirLight.intensity = 0.8;
        
        var shadowGenerator = new BABYLON.ShadowGenerator(2048, dirLight);
        shadowGenerator.useBlurExponentialShadowMap = true;

        this.glowLayer = new BABYLON.GlowLayer("glow", this.scene, { mainTextureRatio: 1, blurKernelSize: 15 });
        this.glowLayer.intensity = 0.8;

        var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 40, height: 40 }, this.scene);
        ground.position.x = 10; ground.position.z = 10;
        ground.renderingGroupId = 0;
        
        var groundMat = new BABYLON.StandardMaterial("groundMat", this.scene);
        groundMat.diffuseColor = new BABYLON.Color3(0.08, 0.08, 0.1); 
        groundMat.specularColor = new BABYLON.Color3(0, 0, 0); 
        ground.material = groundMat;

        this.createDropOffZones();

        this.engine.runRenderLoop(() => { this.scene.render(); });
        window.addEventListener("resize", () => { this.engine.resize(); });
    },

    createDropOffZones: function() {
        for (let i = 0; i < 5; i++) {
            let colorHex = this.zoneColors[i % this.zoneColors.length];
            let color = BABYLON.Color3.FromHexString(colorHex);
            let yPos = 2 + (i * 3);
            
            var zone = BABYLON.MeshBuilder.CreateGround("zone_" + i, { width: 1.4, height: 1.4 }, this.scene);
            zone.position.x = 0; zone.position.z = yPos; zone.position.y = 0.01;
            zone.renderingGroupId = 0;
            
            var zoneMat = new BABYLON.StandardMaterial("zoneMat_" + i, this.scene);
            zoneMat.diffuseColor = color; 
            zoneMat.emissiveColor = new BABYLON.Color3(0,0,0); 
            zoneMat.alpha = 0.6; 
            zone.material = zoneMat;
            this.glowLayer.addExcludedMesh(zone);

            var border = BABYLON.MeshBuilder.CreateTorus("zoneBorder_" + i, { diameter: 1.4, thickness: 0.04, tessellation: 32 }, this.scene);
            border.position.x = 0; border.position.z = yPos; border.position.y = 0.02;
            border.renderingGroupId = 0;
            
            var borderMat = new BABYLON.StandardMaterial("borderMat_" + i, this.scene);
            borderMat.emissiveColor = color; 
            borderMat.diffuseColor = new BABYLON.Color3(0,0,0);
            border.material = borderMat;
            this.glowLayer.addIncludedOnlyMesh(border);
        }
    },

    createMap: function (mapData) {
        // --- DUPLIKÁCIÓ JAVÍTÁSA: Régi polcok törlése ---
        mapData.obstacles.forEach(obs => {
            var existing = this.scene.getMeshByName("obs_" + obs.x + "_" + obs.y);
            if (existing) existing.dispose();
        });

        var shelfMat = new BABYLON.StandardMaterial("shelfBaseMat", this.scene);
        shelfMat.diffuseColor = new BABYLON.Color3(0.2, 0.2, 0.25); 
        shelfMat.specularColor = new BABYLON.Color3(0, 0, 0); 
        shelfMat.emissiveColor = new BABYLON.Color3(0, 0, 0); 
        shelfMat.alpha = 1.0; 
        shelfMat.transparencyMode = BABYLON.Material.MATERIAL_OPAQUE; 

        mapData.obstacles.forEach(obs => {
            var shelf = BABYLON.MeshBuilder.CreateBox("obs_" + obs.x + "_" + obs.y, { 
                width: 0.9, depth: 0.9, height: 2
            }, this.scene);

            shelf.position.x = obs.x; shelf.position.z = obs.y; shelf.position.y = 1;
            shelf.renderingGroupId = 0;

            shelf.material = shelfMat.clone("shelfMat_" + obs.x + "_" + obs.y);
            this.glowLayer.addExcludedMesh(shelf);
        });
    },

    updateRobots: function (robotsData) {
        this.clearShelfHighlights();

        robotsData.forEach(robot => {
            var mesh = this.robotMeshes[robot.id];

            if (!mesh) {
                mesh = BABYLON.MeshBuilder.CreateBox("robot_" + robot.id, { size: 0.8 }, this.scene);
                var mat = new BABYLON.StandardMaterial("mat_" + robot.id, this.scene);
                var color = BABYLON.Color3.FromHexString(robot.colorHex);
                mat.emissiveColor = color.scale(0.8); 
                mat.diffuseColor = new BABYLON.Color3(0, 0, 0);
                mesh.material = mat;
                
                // Röntgen mód bekapcsolva: Mindig látszik a falak mögött is
                mesh.renderingGroupId = 1; 

                this.glowLayer.addIncludedOnlyMesh(mesh);

                var cargoBox = BABYLON.MeshBuilder.CreateBox("cargo_" + robot.id, { size: 0.4 }, this.scene);
                cargoBox.parent = mesh; cargoBox.position.y = 0.7; cargoBox.renderingGroupId = 1;

                var cargoMat = new BABYLON.StandardMaterial("cargoMat", this.scene);
                cargoMat.emissiveColor = new BABYLON.Color3(0.9, 0.9, 0.9); 
                cargoBox.material = cargoMat;
                mesh.cargoMesh = cargoBox;
                this.glowLayer.addIncludedOnlyMesh(cargoBox);

                this.robotMeshes[robot.id] = mesh;
            }

            mesh.position.x = BABYLON.Scalar.Lerp(mesh.position.x, robot.x, 0.2);
            mesh.position.z = BABYLON.Scalar.Lerp(mesh.position.z, robot.y, 0.2);
            mesh.position.y = 0.4 + Math.sin(Date.now() * 0.005 + robot.id) * 0.02;

            if (mesh.cargoMesh) {
                mesh.cargoMesh.isVisible = robot.hasCargo;
                if(robot.hasCargo) mesh.cargoMesh.rotation.y += 0.05;
            }

            if (robot.currentTargetNode) {
                this.highlightShelf(robot.currentTargetNode.x, robot.currentTargetNode.y, robot.colorHex);
            }
        });
    },

    highlightShelf: function(x, y, colorHex) {
        var shelfId = "obs_" + x + "_" + y;
        var shelfMesh = this.scene.getMeshByName(shelfId);
        
        if (shelfMesh) {
            var color = BABYLON.Color3.FromHexString(colorHex);
            shelfMesh.material.emissiveColor = color.scale(0.8);
            this.glowLayer.removeExcludedMesh(shelfMesh);
            this.glowLayer.addIncludedOnlyMesh(shelfMesh);
            this.highlightedShelves.push(shelfMesh);
        }
    },

    clearShelfHighlights: function() {
        this.highlightedShelves.forEach(mesh => {
            mesh.material.emissiveColor = new BABYLON.Color3(0, 0, 0);
            this.glowLayer.removeIncludedOnlyMesh(mesh);
            this.glowLayer.addExcludedMesh(mesh);
        });
        this.highlightedShelves = [];
    }
};