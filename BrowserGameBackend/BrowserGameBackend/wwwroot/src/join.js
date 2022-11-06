"use strict";
// get list of existing games
const maps = await fetch("https://localhost:7237/game/list");
const json = await maps.json();
const joinOptions = {};

// table listing existing games
let innerHTML = `
            <div class="tableFixHead">
                <table>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Map</th>
                            <th>Players</th>
                        </tr>
                    </thead>
                    <tbody>`;

json.forEach((e, idx) => {
    innerHTML += `
                        <tr id="tableRow_${idx}" class="tableRow">
                            <td>${e.name}</td>
                            <td>${e.mapName}</td>
                            <td>${e.playersCount} / ${e.maxPlayers}</td>
                        </tr>`;
})

innerHTML += `
                    </tbody>
                </table>
            </div>`;

// form for getting player's name
innerHTML += `
            <input type="text" id="playerName" name="playerName" placeholder="your name"/>
            <input type="submit" value="create game" id="joinGame" disabled/>
            `;

document.getElementById("menuContainer").innerHTML = innerHTML;

// allow selecting game by clicking on table's row
document.querySelectorAll(".tableRow").forEach(e => {
    e.addEventListener("click", (event) => {   
    
        const target = event.target.parentElement;
    
        document.querySelectorAll(".tableSelectedRow").forEach(e => {
            e.classList.remove("tableSelectedRow");
        });
    
        target.classList.add("tableSelectedRow");
        joinOptions.game = json[parseInt(target.id.substring(target.id.indexOf('_') + 1))].id;
        joinOptions.map = json[parseInt(target.id.substring(target.id.indexOf('_') + 1))].mapName;

        if(joinOptions.game != undefined && joinOptions.playerName != undefined && joinOptions.map != undefined){
            document.getElementById("joinGame").disabled = false;
        }
        else{
            document.getElementById("joinGame").disabled = true;
        }
    });
});

// save player's name
document.getElementById("playerName").addEventListener("change", () => {
    joinOptions.playerName = document.getElementById("playerName").value == "" ? undefined : document.getElementById("playerName").value;

    if(joinOptions.game != undefined && joinOptions.playerName != undefined){
        document.getElementById("joinGame").disabled = false;
    }
    else{
        document.getElementById("joinGame").disabled = true;
    }
});

// redirect to /game page where game will be joined
document.getElementById("joinGame").addEventListener("click", () => {
        window.location.href = `/game.html?action=join&playerName=${joinOptions.playerName}&gameId=${joinOptions.game}`;
})
