window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {},
    shadowGenerator: null,

    init: function (canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.engine = new BABYLON.Engine(this.canvas, true);
        
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color3(0.02, 0.02, 0.05);
        
        this.scene.fogMode = BABYLON.Scene.FOGMODE_EXP;
        this.scene.fogDensity = 0.02;
        this.scene.fogColor = new BABYLON.Color3(0.02, 0.02, 0.05);

        var camera = new BABYLON.ArcRotateCamera("Camera", -Math.PI / 2, Math.PI / 3, 30, new BABYLON.Vector3(10, 0, 10), this.scene);
        camera.attachControl(this.canvas, true);
        camera.wheelPrecision = 50; 

        var hemiLight = new BABYLON.HemisphericLight("hemiLight", new BABYLON.Vector3(0, 1, 0), this.scene);
        hemiLight.intensity = 0.3;
        hemiLight.diffuse = new BABYLON.Color3(0.2, 0.2, 0.5); 

        var dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-1, -2, -1), this.scene);
        dirLight.position = new BABYLON.Vector3(20, 40, 20);
        dirLight.intensity = 0.8;

        this.shadowGenerator = new BABYLON.ShadowGenerator(1024, dirLight);
        this.shadowGenerator.useBlurExponentialShadowMap = true;
        this.shadowGenerator.blurKernel = 32;

        var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 40, height: 40 }, this.scene);
        ground.position.x = 10;
        ground.position.z = 10;
        ground.receiveShadows = true;

        var groundMat = new BABYLON.StandardMaterial("groundMat", this.scene);
        groundMat.diffuseColor = new BABYLON.Color3(0, 0, 0); 
        groundMat.specularColor = new BABYLON.Color3(0.2, 0.2, 0.2); 
        groundMat.emissiveColor = new BABYLON.Color3(0.05, 0.05, 0.1); 
        ground.material = groundMat;

        var gl = new BABYLON.GlowLayer("glow", this.scene);
        gl.intensity = 1.2;

        this.engine.runRenderLoop(() => {
            this.scene.render();
        });

        window.addEventListener("resize", () => {
            this.engine.resize();
        });
    },

    createMap: function (mapData) {
        var shelfMat = new BABYLON.StandardMaterial("shelfMat", this.scene);
        shelfMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.2); 
        shelfMat.specularColor = new BABYLON.Color3(0.5, 0.5, 0.5); 
        
        mapData.obstacles.forEach(obs => {
            var shelf = BABYLON.MeshBuilder.CreateBox("obs_" + obs.x + "_" + obs.y, { 
                width: 0.9, 
                depth: 0.9, 
                height: 2.5 
            }, this.scene);

            shelf.position.x = obs.x;
            shelf.position.z = obs.y;
            shelf.position.y = 1.25;
            
            shelf.material = shelfMat;
            shelf.receiveShadows = true;
            this.shadowGenerator.addShadowCaster(shelf); 
        });
    },

    highlightedShelves: {},

    // Robotok frissítése
    updateRobots: function (robotsData) {
        this.clearShelfHighlights();

        robotsData.forEach(robot => {
            var mesh = this.robotMeshes[robot.id];

            if (!mesh) {
                mesh = BABYLON.MeshBuilder.CreateBox("robot_" + robot.id, { size: 0.8 }, this.scene);
                
                var mat = new BABYLON.StandardMaterial("mat_" + robot.id, this.scene);
                mat.emissiveColor = BABYLON.Color3.FromHexString(robot.colorHex); 
                mat.diffuseColor = new BABYLON.Color3(0, 0, 0);
                mesh.material = mat;
                
                this.shadowGenerator.addShadowCaster(mesh);

                var cargoBox = BABYLON.MeshBuilder.CreateBox("cargo_" + robot.id, { size: 0.4 }, this.scene);
                cargoBox.parent = mesh;
                cargoBox.position.y = 0.7;
                
                var cargoMat = new BABYLON.StandardMaterial("cargoMat", this.scene);
                cargoMat.emissiveColor = new BABYLON.Color3(1, 1, 1); 
                cargoBox.material = cargoMat;
                cargoBox.isVisible = false;
                mesh.cargoMesh = cargoBox;

                this.robotMeshes[robot.id] = mesh;
            }

            mesh.position.x = BABYLON.Scalar.Lerp(mesh.position.x, robot.x, 0.15);
            mesh.position.z = BABYLON.Scalar.Lerp(mesh.position.z, robot.y, 0.15);
            mesh.position.y = 0.4 + Math.sin(Date.now() * 0.005 + robot.id) * 0.05; 


            if (mesh.cargoMesh) {
                mesh.cargoMesh.isVisible = robot.hasCargo;
                if(robot.hasCargo) {
                    mesh.cargoMesh.rotation.y += 0.05; 
                }
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
            shelfMesh.material.emissiveColor = BABYLON.Color3.FromHexString(colorHex);
            this.highlightedShelves[shelfId] = shelfMesh;
        }
    },

    clearShelfHighlights: function() {
        for (var id in this.highlightedShelves) {
            var mesh = this.highlightedShelves[id];
            mesh.material.emissiveColor = new BABYLON.Color3(0, 0, 0);
        }
        this.highlightedShelves = {};
    }
};