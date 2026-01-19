window.warehouseVisualizer = {
    canvas: null,
    engine: null,
    scene: null,
    robotMeshes: {},

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

    updateRobots: function (robotsData) {
        robotsData.forEach(robot => {
            if (!this.robotMeshes[robot.id]) {
                var box = BABYLON.MeshBuilder.CreateBox("robot_" + robot.id, { size: 0.8 }, this.scene);
                
                var material = new BABYLON.StandardMaterial("mat_" + robot.id, this.scene);
                material.emissiveColor = BABYLON.Color3.FromHexString(robot.colorHex);
                box.material = material;
                
                this.robotMeshes[robot.id] = box;
            }

            var mesh = this.robotMeshes[robot.id];
            
            mesh.position.x = robot.x;
            mesh.position.z = robot.y; 
            mesh.position.y = 0.4;
        });
    }
};