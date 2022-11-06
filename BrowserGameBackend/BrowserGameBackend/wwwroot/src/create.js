"use strict";

// get list of avaible maps
const maps = await fetch("https://localhost:7237/map/list");
const json = await maps.json();
const creationOptions = {};

// table listing avaible maps
let innerHTML = `
            <div class="tableFixHead">
                <table>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Size</th>
                        </tr>
                    </thead>
                    <tbody>`;

json.forEach((e, idx) => {
    innerHTML += `
                        <tr id="tableRow_${idx}" class="tableRow">
                            <td>${e.name}</td>
                            <td>${e.width} x ${e.height}</td>
                        </tr>`;
})

innerHTML += `
                    </tbody>
                </table>
            </div>`;

// form for getting game's and player's name
innerHTML += `
            <input type="text" id="playerName" name="playerName" placeholder="your name"/>
            <input type="text" id="gameName" name="gameName" placeholder="game's name"/>
            <input type="submit" value="create game" id="createGame" disabled/>
            `;

document.getElementById("menuContainer").innerHTML = innerHTML;

// allow selecting map by clicking on table's row
document.querySelectorAll(".tableRow").forEach(e => {
    e.addEventListener("click", (event) => {   
    
        const target = event.target.parentElement;
    
        document.querySelectorAll(".tableSelectedRow").forEach(e => {
            e.classList.remove("tableSelectedRow");
        });
    
        target.classList.add("tableSelectedRow");
        creationOptions.map = json[parseInt(target.id.substring(target.id.indexOf('_') + 1))].name;

        if(creationOptions.map != undefined && creationOptions.playerName != undefined && creationOptions.gameName != undefined){
            document.getElementById("createGame").disabled = false;
        }
        else{
            document.getElementById("createGame").disabled = true;
        }
    });
});

// save player's name
document.getElementById("playerName").addEventListener("change", () => {
    creationOptions.playerName = document.getElementById("playerName").value === "" ? undefined : document.getElementById("playerName").value;

    if(creationOptions.map != undefined && creationOptions.playerName != undefined && creationOptions.gameName != undefined){
        document.getElementById("createGame").disabled = false;
    }
    else{
        document.getElementById("createGame").disabled = true;
    }
});

// save game's name
document.getElementById("gameName").addEventListener("change", () => {
    creationOptions.gameName = document.getElementById("gameName").value === "" ? undefined : document.getElementById("gameName").value;

    if(creationOptions.map != undefined && creationOptions.playerName != undefined && creationOptions.gameName != undefined){
        document.getElementById("createGame").disabled = false;
    }
    else{
        document.getElementById("createGame").disabled = true;
    }
});

// redirect to /game page where game will be created
document.getElementById("createGame").addEventListener("click", () => {
        window.location.href = `/game.html?action=create&playerName=${creationOptions.playerName}&gameName=${creationOptions.gameName}&map=${creationOptions.map}`;
})
