window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {}, 
    pathLines: {},     // Útvonal vonalak tárolása
    targetMarkers: {}, // Célkeresztek tárolása
    shadowGenerator: null,
    mirrorTexture: null, 
    dotNetHelper: null, 
    
    zoneColors: ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"],

    init: function (canvasId, dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.robotMeshes = {}; 
        this.pathLines = {};
        this.targetMarkers = {};
        this.highlightedShelves = [];

        this.canvas = document.getElementById(canvasId);
        this.engine = new BABYLON.Engine(this.canvas, true, { preserveDrawingBuffer: true, antialias: true });
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color4(0, 0, 0, 0);
        this.scene.fogMode = BABYLON.Scene.FOGMODE_EXP2;
        this.scene.fogDensity = 0.015;
        this.scene.fogColor = new BABYLON.Color3(0.01, 0.015, 0.025);
        var camera = new BABYLON.ArcRotateCamera("Camera", Math.PI / 2, Math.PI / 3, 24, new BABYLON.Vector3(10, 0, 10), this.scene);
        camera.attachControl(this.canvas, true);
        camera.wheelPrecision = 50;
        camera.minZ = 0.5;

        // --- KATTINTÁS KEZELÉS ---
        this.scene.onPointerDown = (evt, pickResult) => {
            if (pickResult.hit && pickResult.pickedMesh) {
                var mesh = pickResult.pickedMesh;
                
                // 1. Robotra kattintás
                var root = mesh;
                while (root.parent) root = root.parent;
                if (root.name && root.name.startsWith("root_")) {
                    var id = parseInt(root.name.split("_")[1]);
                    if (this.dotNetHelper) this.dotNetHelper.invokeMethodAsync("SelectRobotFromJS", id);
                    return; 
                }

                // 2. Padlóra kattintás (ha ground a neve)
                if (mesh.name === "ground") {
                    var x = Math.round(pickResult.pickedPoint.x);
                    var y = Math.round(pickResult.pickedPoint.z); // Z a mélység Babylonban!
                    if (this.dotNetHelper) {
                        this.dotNetHelper.invokeMethodAsync("FloorClickFromJS", x, y);
                    }
                }
            }
        };

        var hemiLight = new BABYLON.HemisphericLight("hemiLight", new BABYLON.Vector3(0, 1, 0), this.scene);
        hemiLight.intensity = 0.4; 
        var dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-1, -2, -1), this.scene);
        dirLight.position = new BABYLON.Vector3(20, 40, 20);
        dirLight.intensity = 0.8;

        this.shadowGenerator = new BABYLON.ShadowGenerator(1024, dirLight);
        this.shadowGenerator.useBlurExponentialShadowMap = true;
        this.shadowGenerator.blurKernel = 16; 

        var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 50, height: 50 }, this.scene);
        ground.position.x = 10; ground.position.z = 10; ground.receiveShadows = true;

        this.mirrorTexture = new BABYLON.MirrorTexture("mirror", 512, this.scene, true);
        this.mirrorTexture.mirrorPlane = new BABYLON.Plane(0, -1, 0, 0);
        this.mirrorTexture.level = 0.35; 

        var groundMat = new BABYLON.StandardMaterial("groundMat", this.scene);
        groundMat.diffuseColor = new BABYLON.Color3(0.05, 0.05, 0.05); 
        groundMat.reflectionTexture = this.mirrorTexture; 
        groundMat.specularColor = new BABYLON.Color3(0.2, 0.2, 0.2); 
        groundMat.alpha = 0.15;
        ground.material = groundMat;

        this.createDropOffZones();

        this.engine.runRenderLoop(() => {
            if (this.scene && this.scene.activeCamera) {
                this.animateDrones(); 
                this.scene.render();
            }
        });
        window.addEventListener("resize", () => { this.engine.resize(); });
    },

    animateDrones: function() {
        var now = Date.now();
        for (var id in this.robotMeshes) {
            var robotObj = this.robotMeshes[id];
            
            // Marker forgatása
            if (this.targetMarkers[id]) {
                this.targetMarkers[id].rotation.y += 0.02;
            }

            if (robotObj.metadata && robotObj.metadata.state !== "Charging") {
                if (robotObj.propellers) robotObj.propellers.forEach(p => p.rotation.y += 0.8);
            }

            if (robotObj.metadata) {
                var mat = robotObj.lightMat;
                if (robotObj.metadata.state === "Charging") {
                    var pulse = 0.5 + (Math.sin(now * 0.005) * 0.5 + 0.5); 
                    mat.emissiveColor = new BABYLON.Color3(1, 1, 1).scale(pulse * 2.0);
                }
                else if (robotObj.metadata.hasCargo) {
                    var intensity = Math.sin(now * 0.025) > 0.2 ? 8.0 : 0.2; 
                    if (robotObj.cargoMesh) robotObj.cargoMesh.rotation.y += 0.1;
                    mat.emissiveColor = robotObj.metadata.baseColor.scale(intensity);
                } 
                else {
                    mat.emissiveColor = robotObj.metadata.baseColor.scale(1.5);
                }
            }
        }
    },

    updatePathVisuals: function(robot) {
        var id = robot.id;
        var color = BABYLON.Color3.FromHexString(robot.colorHex);

        if (robot.state !== "Idle" && robot.state !== "Charging") {
            if (!this.targetMarkers[id]) {
                var marker = BABYLON.MeshBuilder.CreateTorus("target_" + id, { diameter: 0.8, thickness: 0.1 }, this.scene);
                var mat = new BABYLON.StandardMaterial("targetMat_" + id, this.scene);
                mat.emissiveColor = color;
                mat.disableLighting = true;
                marker.material = mat;
                this.targetMarkers[id] = marker;
            }
            var m = this.targetMarkers[id];
            m.position.x = robot.targetX;
            m.position.z = robot.targetY;
            m.position.y = 0.05;
            m.isVisible = true;
        } else {
            if (this.targetMarkers[id]) this.targetMarkers[id].isVisible = false;
        }
        if (this.pathLines[id]) {
            this.pathLines[id].isVisible = false;
        }
    },

    updateRobots: function (robotsData) {
        this.clearShelfHighlights();

        robotsData.forEach(robot => {
            var robotObj = this.robotMeshes[robot.id];

            if (!robotObj) {
                robotObj = this.createDroneMesh(robot.id, robot.colorHex);
                robotObj.mesh.getChildMeshes().forEach(m => {
                    this.shadowGenerator.addShadowCaster(m);
                    this.mirrorTexture.renderList.push(m);
                });
                robotObj.metadata = { 
                    baseColor: BABYLON.Color3.FromHexString(robot.colorHex),
                    hasCargo: false,
                    state: "Idle"
                };
                this.robotMeshes[robot.id] = robotObj;
            }

            var root = robotObj.mesh;
            robotObj.metadata.hasCargo = robot.hasCargo;
            robotObj.metadata.state = robot.state; 

            this.updatePathVisuals(robot);

            var targetY;
            if (robot.state === "Charging") {
                targetY = 0.2;
            } else {
                targetY = 1.8 + Math.sin(Date.now() * 0.003 + robot.id) * 0.1;
            }

            root.position.x = BABYLON.Scalar.Lerp(root.position.x, robot.x, 0.2);
            root.position.z = BABYLON.Scalar.Lerp(root.position.z, robot.y, 0.2);
            root.position.y = BABYLON.Scalar.Lerp(root.position.y, targetY, 0.1); 

            if (robotObj.cargoMesh) robotObj.cargoMesh.isVisible = robot.hasCargo;
            if (robot.currentTargetNode) this.highlightShelf(robot.currentTargetNode.x, robot.currentTargetNode.y, robot.colorHex);
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
            zoneMat.diffuseColor = color; zoneMat.emissiveColor = color.scale(0.3); zoneMat.alpha = 0.8; zone.material = zoneMat;
            var border = BABYLON.MeshBuilder.CreateTorus("zoneBorder_" + i, { diameter: 1.4, thickness: 0.05, tessellation: 32 }, this.scene);
            border.position.x = 0; border.position.z = yPos; border.position.y = 0.03;
            var borderMat = new BABYLON.StandardMaterial("borderMat_" + i, this.scene);
            borderMat.emissiveColor = color; borderMat.diffuseColor = new BABYLON.Color3(0,0,0); border.material = borderMat;
            this.mirrorTexture.renderList.push(border);
        }
    },
    createMap: function (mapData) {
        mapData.obstacles.forEach(obs => { var existing = this.scene.getMeshByName("obs_" + obs.x + "_" + obs.y); if (existing) existing.dispose(); });
        var shelfMat = new BABYLON.StandardMaterial("shelfMat", this.scene);
        shelfMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.2); shelfMat.specularColor = new BABYLON.Color3(0.3, 0.3, 0.3); shelfMat.emissiveColor = new BABYLON.Color3(0, 0, 0); 
        mapData.obstacles.forEach(obs => {
            var shelf = BABYLON.MeshBuilder.CreateBox("obs_" + obs.x + "_" + obs.y, { width: 0.9, depth: 0.9, height: 2.2 }, this.scene);
            shelf.position.x = obs.x; shelf.position.z = obs.y; shelf.position.y = 1.1; shelf.material = shelfMat; shelf.receiveShadows = true;
            this.shadowGenerator.addShadowCaster(shelf); this.mirrorTexture.renderList.push(shelf);
        });
    },
    createDroneMesh: function(id, colorHex) {
        var color = BABYLON.Color3.FromHexString(colorHex);
        var root = new BABYLON.TransformNode("root_" + id, this.scene);
        var darkMetal = new BABYLON.StandardMaterial("darkMetal", this.scene); darkMetal.diffuseColor = new BABYLON.Color3(0.1, 0.1, 0.15);
        var lightMat = new BABYLON.StandardMaterial("lightMat_" + id, this.scene); lightMat.emissiveColor = color.scale(2.0); lightMat.diffuseColor = new BABYLON.Color3(0,0,0);
        var propMat = new BABYLON.StandardMaterial("propMat", this.scene); propMat.diffuseColor = new BABYLON.Color3(0.8, 0.8, 0.9); propMat.alpha = 0.6; 
        var body = BABYLON.MeshBuilder.CreateCylinder("body", { diameter: 0.4, height: 0.15 }, this.scene); body.material = darkMetal; body.parent = root;
        var hitBox = BABYLON.MeshBuilder.CreateBox("hitbox_" + id, {size: 1.5}, this.scene); hitBox.parent = root; hitBox.isVisible = false; 
        var ring = BABYLON.MeshBuilder.CreateTorus("ring", { diameter: 0.45, thickness: 0.05 }, this.scene); ring.material = lightMat; ring.parent = root;
        var arm1 = BABYLON.MeshBuilder.CreateBox("arm1", { width: 1.2, height: 0.05, depth: 0.1 }, this.scene); arm1.rotation.y = Math.PI / 4; arm1.material = darkMetal; arm1.parent = root;
        var arm2 = arm1.clone("arm2"); arm2.rotation.y = -Math.PI / 4; arm2.parent = root;
        var propellers = [];
        var positions = [{ x: 0.45, z: 0.45 }, { x: -0.45, z: -0.45 }, { x: 0.45, z: -0.45 }, { x: -0.45, z: 0.45 }];
        positions.forEach((pos, idx) => {
            var motor = BABYLON.MeshBuilder.CreateCylinder("motor"+idx, { diameter: 0.1, height: 0.1 }, this.scene); motor.position.x = pos.x; motor.position.z = pos.z; motor.position.y = 0.05; motor.material = darkMetal; motor.parent = root;
            var prop = BABYLON.MeshBuilder.CreateBox("prop"+idx, { width: 0.6, depth: 0.05, height: 0.01 }, this.scene); prop.position.y = 0.06; prop.material = propMat; prop.parent = motor; propellers.push(prop);
        });
        var cargo = BABYLON.MeshBuilder.CreateBox("cargo", { size: 0.35 }, this.scene); cargo.position.y = -0.3; cargo.parent = root;
        var cargoMat = new BABYLON.StandardMaterial("cargoMat", this.scene); cargoMat.emissiveColor = new BABYLON.Color3(1, 1, 1); cargo.material = cargoMat; cargo.isVisible = false; 
        return { mesh: root, lightMat: lightMat, cargoMesh: cargo, propellers: propellers };
    },
    highlightShelf: function(x, y, colorHex) {
        var shelfId = "obs_" + x + "_" + y;
        var shelfMesh = this.scene.getMeshByName(shelfId);
        if (shelfMesh) {
            if (shelfMesh.material.name === "shelfMat") shelfMesh.material = shelfMesh.material.clone("highlightMat_" + x + "_" + y);
            var color = BABYLON.Color3.FromHexString(colorHex);
            shelfMesh.material.emissiveColor = color.scale(0.8);
            this.highlightedShelves.push(shelfMesh);
        }
    },
    clearShelfHighlights: function() {
        this.highlightedShelves.forEach(mesh => { mesh.material.emissiveColor = new BABYLON.Color3(0, 0, 0); });
        this.highlightedShelves = [];
    }
};