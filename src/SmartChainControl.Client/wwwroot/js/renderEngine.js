window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {},

    // Initialize Babylon.js 3D scene with camera, lighting, and ground plane
    init: function (canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.engine = new BABYLON.Engine(this.canvas, true);
        
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color3(0.05, 0.05, 0.1);

        var camera = new BABYLON.ArcRotateCamera("Camera", -Math.PI / 2, Math.PI / 3, 25, new BABYLON.Vector3(10, 0, 10), this.scene);
        camera.attachControl(this.canvas, true);

        var light = new BABYLON.HemisphericLight("light1", new BABYLON.Vector3(0, 1, 0), this.scene);
        light.intensity = 0.7;

        var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 20, height: 20 }, this.scene);
        ground.position.x = 10;
        ground.position.z = 10;
        
        var gridMaterial = new BABYLON.StandardMaterial("gridMat", this.scene);
        gridMaterial.wireframe = true;
        gridMaterial.emissiveColor = new BABYLON.Color3(0, 0.2, 0.4);
        ground.material = gridMaterial;

        this.engine.runRenderLoop(() => {
            this.scene.render();
        });

        window.addEventListener("resize", () => {
            this.engine.resize();
        });
    },

    // Generate 3D shelf meshes from map obstacle data
    createMap: function (mapData) {
        mapData.obstacles.forEach(obs => {
            var shelf = BABYLON.MeshBuilder.CreateBox("obs_" + obs.x + "_" + obs.y, { 
                width: 1, 
                depth: 1, 
                height: 2
            }, this.scene);

            shelf.position.x = obs.x;
            shelf.position.z = obs.y;
            shelf.position.y = 1;

            var shelfMat = new BABYLON.StandardMaterial("shelfMat", this.scene);
            shelfMat.diffuseColor = new BABYLON.Color3(0.3, 0.3, 0.35);
            shelfMat.emissiveColor = new BABYLON.Color3(0.1, 0.1, 0.15);
            shelf.material = shelfMat;
        });
    },

    highlightedShelves: {},

    // Update robot positions and cargo visibility with smooth interpolation
    updateRobots: function (robotsData) {
        this.clearShelfHighlights();

        robotsData.forEach(robot => {
            var mesh = this.robotMeshes[robot.id];

            // Create robot mesh with cargo box on first encounter
            if (!mesh) {
                mesh = BABYLON.MeshBuilder.CreateBox("robot_" + robot.id, { size: 0.8 }, this.scene);
                var mat = new BABYLON.StandardMaterial("mat_" + robot.id, this.scene);
                mat.emissiveColor = BABYLON.Color3.FromHexString(robot.colorHex);
                mesh.material = mat;
                
                var cargoBox = BABYLON.MeshBuilder.CreateBox("cargo_" + robot.id, { size: 0.5 }, this.scene);
                cargoBox.parent = mesh;
                cargoBox.position.y = 0.7;
                
                var cargoMat = new BABYLON.StandardMaterial("cargoMat", this.scene);
                cargoMat.diffuseColor = new BABYLON.Color3(0, 1, 0);
                cargoMat.emissiveColor = new BABYLON.Color3(0, 0.5, 0);
                cargoBox.material = cargoMat;
                cargoBox.isVisible = false;
                mesh.cargoMesh = cargoBox;

                this.robotMeshes[robot.id] = mesh;
            }

            mesh.position.x = BABYLON.Scalar.Lerp(mesh.position.x, robot.x, 0.2);
            mesh.position.z = BABYLON.Scalar.Lerp(mesh.position.z, robot.y, 0.2);

            if (mesh.cargoMesh) {
                mesh.cargoMesh.isVisible = robot.hasCargo;
            }

            if (robot.currentTargetNode) {
                this.highlightShelf(robot.currentTargetNode.x, robot.currentTargetNode.y, robot.colorHex);
            }
        });
    },

    // Highlight target shelf with robot's color
    highlightShelf: function(x, y, colorHex) {
        var shelfId = "obs_" + x + "_" + y;
        var shelfMesh = this.scene.getMeshByName(shelfId);
        
        if (shelfMesh) {
            shelfMesh.material.emissiveColor = BABYLON.Color3.FromHexString(colorHex);
            this.highlightedShelves[shelfId] = shelfMesh;
        }
    },

    clearShelfHighlights: function() {
        for (var id in this.highlightedShelves) {
            var mesh = this.highlightedShelves[id];
            mesh.material.emissiveColor = new BABYLON.Color3(0.1, 0.1, 0.15);
        }
        this.highlightedShelves = {};
    }
};