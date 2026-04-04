<%@ Page Title="Route Planner" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RouteDistanceCalculation.aspx.cs" Inherits="AnmolDristi.RouteDistanceCalculation" Async="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" 
      integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=" 
      crossorigin="anonymous" />
    <style>
        .spinner-overlay {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(255,255,255,0.8);
            z-index: 1000;
            display: flex;
            justify-content: center;
            align-items: center;
            flex-direction: column;
            border-radius: 4px;
        }

        .map-wrapper {
            position: relative;
            height: 600px;
            width: 100%;
        }

        #map {
            height: 100%;
            width: 100%;
            border-radius: 4px;
            border: 1px solid #ccc;
            z-index: 1;
        }

        /* Custom CSS for Numbered Map Pins */
        .number-marker {
            background-color: #1ABB9C; /* Match Gentelella theme */
            color: white;
            border-radius: 50%;
            width: 30px;
            height: 30px;
            display: flex;
            justify-content: center;
            align-items: center;
            font-weight: bold;
            font-size: 14px;
            border: 2px solid white;
            box-shadow: 0px 2px 5px rgba(0,0,0,0.5);
        }
        /* Style for the search result pins (un-numbered) */
        .search-marker {
            background-color: #e74c3c;
            color: white;
            border-radius: 50%;
            width: 20px;
            height: 20px;
            border: 2px solid white;
            box-shadow: 0px 2px 5px rgba(0,0,0,0.5);
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hfTotalDistance" runat="server" />
    <asp:HiddenField ID="hfTravelMode" runat="server" />
    <asp:HiddenField ID="hfOriginName" runat="server" />
    <asp:HiddenField ID="hfDestinationName" runat="server" />
    <asp:HiddenField ID="hfWaypointsJSON" runat="server" />

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Log Travel Route for Approval</h5>
                </div>
            </div>
            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-4 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Route Details</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="form-group">
                                <label>Search Location</label>
                                <div class="input-group">
                                    <input type="text" id="txtSearch" class="form-control form-control-sm rounded" placeholder="Search..." />
                                    <span class="input-group-btn">
                                        <button type="button" class="btn btn-primary btn-sm" onclick="searchPlace()">Find</button>
                                    </span>
                                </div>
                            </div>
                            <div class="form-group">
                                <label>Travel Mode</label>
                                <select id="ddlMode" class="form-control form-control-sm rounded" onchange="calculateAndDrawRoute()">
                                    <option value="driving">Car (Driving)</option>
                                    <option value="bike">Bicycle (Bike)</option>
                                    <option value="foot">Walking (Foot)</option>
                                </select>
                            </div>

                            <div class="ln_solid"></div>

                            <h5>Your Stops</h5>
                            <ul id="waypointList" class="list-unstyled" style="background: #f9f9f9; padding: 10px; border-radius: 4px;">
                                <li class="text-muted">No stops added. Click map to begin.</li>
                            </ul>

                            <div class="ln_solid"></div>

                            <div class="text-center">
                                <span id="lblStatus" style="color: #d35400; font-weight: bold; display: block; margin-bottom: 5px;"></span>
                                <span id="lblDistance" style="font-size: 1.3em; font-weight: bold; color: #1ABB9C; display: block; margin-bottom: 15px;"></span>

                                <asp:Button ID="btnSubmitApproval" runat="server" Text="Submit Route for Approval" CssClass="btn btn-success" OnClick="btnSubmitApproval_Click" OnClientClick="return ValidateRouteData();" />
                            </div>
                            <asp:Label ID="lblServerMessage" runat="server" Font-Bold="true"></asp:Label>
                        </div>
                    </div>
                </div>

                <div class="col-md-8 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Visual Map View</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="map-wrapper">
                                <div id="map-spinner" class="spinner-overlay">
                                    <i class="fa fa-spinner fa-spin fa-3x fa-fw" style="color: #1ABB9C;"></i>
                                    <span style="margin-top: 10px; font-weight: bold; color: #1ABB9C;">Loading Map...</span>
                                </div>
                                <div id="map"></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="row">
            <div class="col-md-12 col-sm-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>Pending Route Approvals</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <div class="table-responsive">
                            <table class="table table-striped table-bordered">
                                <thead>
                                    <tr>
                                        <th>Route ID</th>
                                        <th>Submitted On</th>
                                        <th>Origin</th>
                                        <th>Destination</th>
                                        <th>Mode</th>
                                        <th>Distance</th>
                                        <th class="text-center">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptPendingRoutes" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%# Eval("RouteID") %></td>
                                                <td><%# Convert.ToDateTime(Eval("SubmittedDate")).ToString("dd-MMM-yyyy") %></td>
                                                <td><%# Eval("OriginName") %></td>
                                                <td><%# Eval("DestinationName") %></td>
                                                <td style="text-transform: capitalize;"><%# Eval("TravelMode") %></td>
                                                <td><%# Eval("TotalDistanceKM") %> KM</td>
                                                <td class="text-center">
                                                    <button type="button" class="btn btn-info btn-xs"
                                                        data-waypoints='<%# HttpUtility.HtmlEncode(Eval("WaypointsJSON").ToString()) %>'
                                                        data-mode='<%# Eval("TravelMode") %>'
                                                        onclick="loadSavedRoute(this)">
                                                        <i class="fa fa-map-marker"></i>View on Map
                                                    </button>
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <asp:Label ID="lblNoRecords" runat="server" Visible="false" Text="<tr><td colspan='7' class='text-center'>No pending routes found.</td></tr>"></asp:Label>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" 
        integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=" 
        crossorigin="anonymous"></script>

    <script type="text/javascript">
        var map = L.map('map').setView([22.68, 87.20], 8);
        var tileLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);

        tileLayer.on('load', function() {
            var spinner = document.getElementById('map-spinner');
            if(spinner) spinner.style.display = 'none';
        });

        setTimeout(function() { map.invalidateSize(); }, 500);

        var routeLayer = null;
        var waypoints = [];       
        var currentRoutePins = []; 
        var searchPins = [];       

        // Factory to create numbered icons
        function getNumberedIcon(number) {
            return L.divIcon({
                className: 'custom-icon',
                html: `<div class="number-marker">${number}</div>`,
                iconSize: [30, 30],
                iconAnchor: [15, 15] // Center the div directly over the coordinate
            });
        }

        async function searchPlace() {
            // 1. Get UI Elements
            // Note: Assuming your 'Find' button is the first button inside the input-group
            var btnFind = document.querySelector('.input-group-btn button'); 
            var query = document.getElementById('txtSearch').value.trim();
            var statusLabel = document.getElementById('lblStatus');

            if (!query) return;

            // 2. Lock UI to prevent API spamming
            if (btnFind) {
                btnFind.disabled = true;
                btnFind.innerText = "Wait...";
            }
            if (statusLabel) statusLabel.innerText = "Searching...";

            clearSearchPins();
            var center = map.getCenter();
            var viewbox = `${center.lng - 0.5},${center.lat + 0.5},${center.lng + 0.5},${center.lat - 0.5}`;
            var url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&viewbox=${viewbox}&bounded=0&limit=5`;

            try {
                var response = await fetch(url);
                if (!response.ok) throw new Error("Network response was not ok");
                var data = await response.json();

                if (data.length === 0 && statusLabel) {
                    statusLabel.innerText = "No results found.";
                } else if (statusLabel) {
                    statusLabel.innerText = "";
                }
        
                data.forEach((place, index) => {
                    // Use a red dot for search results so they aren't confused with route numbers
                    var searchIcon = L.divIcon({ className: 'custom-icon', html: `<div class="search-marker"></div>`, iconSize: [20, 20], iconAnchor: [10, 10] });
                    var marker = L.marker([place.lat, place.lon], {icon: searchIcon}).addTo(map);
            
                    var name = place.name || place.display_name.split(',')[0];
            
                    // 3. XSS Mitigation: Build DOM nodes safely instead of using raw HTML strings
                    var popupDiv = document.createElement('div');
                    var title = document.createElement('b');
                    title.appendChild(document.createTextNode(name)); // Safely encodes the text
                    popupDiv.appendChild(title);
                    popupDiv.appendChild(document.createElement('br'));
            
                    var btn = document.createElement('button');
                    btn.className = "btn btn-xs btn-success mt-2";
                    btn.innerText = "Add Stop Here";
                    btn.type = "button";
                    btn.onclick = function() {
                        addWaypoint(place.lat, place.lon, name);
                        map.closePopup();
                        clearSearchPins();
                        document.getElementById('txtSearch').value = '';
                    };
            
                    popupDiv.appendChild(btn);
                    marker.bindPopup(popupDiv);
                    searchPins.push(marker);
            
                    if(index === 0) map.setView([place.lat, place.lon], 13);
                });
        
                if(searchPins.length > 0) searchPins[0].openPopup();

            } catch (err) {
                console.error(err);
                if (statusLabel) statusLabel.innerText = "Search Error. Try again.";
            } finally {
                // 4. Unlock UI after a mandatory 1-second cooldown (Nominatim requirement)
                setTimeout(() => {
                    if (btnFind) {
                        btnFind.disabled = false;
                        btnFind.innerText = "Find";
                    }
                }, 1000);
            }
        }

        // --- ARRAY MANIPULATION LOGIC ---

        function addWaypoint(lat, lon, name) {
            waypoints.push({ lat: parseFloat(lat), lon: parseFloat(lon), name: name });
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function removeWaypoint(index) {
            waypoints.splice(index, 1);
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function moveUp(index) {
            if (index > 0) {
                var temp = waypoints[index];
                waypoints[index] = waypoints[index - 1];
                waypoints[index - 1] = temp;
                updateWaypointListUI();
                calculateAndDrawRoute();
            }
        }

        function moveDown(index) {
            if (index < waypoints.length - 1) {
                var temp = waypoints[index];
                waypoints[index] = waypoints[index + 1];
                waypoints[index + 1] = temp;
                updateWaypointListUI();
                calculateAndDrawRoute();
            }
        }

        // --- UI RENDERING LOGIC ---

        //function updateWaypointListUI() {
        //    var list = document.getElementById('waypointList');
        //    list.innerHTML = ''; 
        //    if (waypoints.length === 0) { list.innerHTML = '<li class="text-muted">No stops added.</li>'; return; }
            
        //    waypoints.forEach((wp, index) => {
        //        var li = document.createElement('li');
        //        li.style.display = "flex";
        //        li.style.justifyContent = "space-between";
        //        li.style.alignItems = "center";
        //        li.style.padding = "8px 5px";
        //        li.style.borderBottom = "1px solid #e5e5e5";
                
        //        // Show sequence number next to the name
        //        var nameHTML = `<span style="flex-grow:1;"><strong style="color:#1ABB9C;">${index + 1}.</strong> ${wp.name}</span>`;
                
        //        // Build the control buttons (Up, Down, Delete)
        //        var buttonsHTML = `<div style="min-width: 80px; text-align:right;">`;
                
        //        if (index > 0) {
        //            buttonsHTML += `<button type="button" class="btn btn-xs btn-default" title="Move Up" onclick="moveUp(${index})"><i class="fa fa-arrow-up"></i></button>`;
        //        }
        //        if (index < waypoints.length - 1) {
        //            buttonsHTML += `<button type="button" class="btn btn-xs btn-default" title="Move Down" onclick="moveDown(${index})"><i class="fa fa-arrow-down"></i></button>`;
        //        }
                
        //        buttonsHTML += `<button type="button" class="btn btn-xs btn-danger" title="Remove" onclick="removeWaypoint(${index})"><i class="fa fa-trash"></i></button></div>`;
                
        //        li.innerHTML = nameHTML + buttonsHTML;
        //        list.appendChild(li);
        //    });
        //}

        function updateWaypointListUI() {
            var list = document.getElementById('waypointList');
            list.innerHTML = ''; 
            if (waypoints.length === 0) { list.innerHTML = '<li class="text-muted">No stops added.</li>'; return; }
            
            waypoints.forEach((wp, index) => {
                var li = document.createElement('li');
                li.style.display = "flex";
                li.style.justifyContent = "space-between";
                li.style.alignItems = "center";
                li.style.padding = "8px 5px";
                li.style.borderBottom = "1px solid #e5e5e5";
                
                // --- NEW: SAFE DOM INJECTION ---
                var textContainer = document.createElement('span');
                textContainer.style.flexGrow = "1";
                
                var numberBold = document.createElement('strong');
                numberBold.style.color = "#1ABB9C";
                numberBold.innerText = `${index + 1}. `;
                
                // Use document.createTextNode to strictly enforce plain text
                var safeNameNode = document.createTextNode(wp.name); 
                
                textContainer.appendChild(numberBold);
                textContainer.appendChild(safeNameNode);
                // -------------------------------

                var buttonsHTML = `<div style="min-width: 80px; text-align:right;">`;
                if (index > 0) buttonsHTML += `<button type="button" class="btn btn-xs btn-default" onclick="moveUp(${index})"><i class="fa fa-arrow-up"></i></button>`;
                if (index < waypoints.length - 1) buttonsHTML += `<button type="button" class="btn btn-xs btn-default" onclick="moveDown(${index})"><i class="fa fa-arrow-down"></i></button>`;
                buttonsHTML += `<button type="button" class="btn btn-xs btn-danger" onclick="removeWaypoint(${index})"><i class="fa fa-trash"></i></button></div>`;
                
                li.appendChild(textContainer);
                li.insertAdjacentHTML('beforeend', buttonsHTML);
                list.appendChild(li);
            });
        }

        async function calculateAndDrawRoute() {
            var distanceLabel = document.getElementById('lblDistance');
            var statusLabel = document.getElementById('lblStatus');
            var mode = document.getElementById('ddlMode').value;
            clearRouteGraphics();

            if (waypoints.length < 2) {
                distanceLabel.innerText = "";
                syncHiddenFields("", mode, ""); 
                
                if (waypoints.length === 1) {
                    // Draw single numbered point
                    var singlePin = L.marker([waypoints[0].lat, waypoints[0].lon], {icon: getNumberedIcon(1)}).bindPopup(waypoints[0].name).addTo(map);
                    currentRoutePins.push(singlePin);
                    map.setView([waypoints[0].lat, waypoints[0].lon], 12);
                }
                return;
            }

            statusLabel.innerText = "Calculating...";

            try {
                var coordsString = waypoints.map(wp => `${wp.lon},${wp.lat}`).join(';');
                var osrmUrl = `https://router.project-osrm.org/route/v1/${mode}/${coordsString}?overview=full&geometries=geojson`;

                var response = await fetch(osrmUrl);
                var routeData = await response.json();

                if (routeData.code !== "Ok") throw new Error("Routing failed");

                var distanceKm = (routeData.routes[0].distance / 1000).toFixed(2);
                drawActualRoute(routeData.routes[0].geometry);
                
                statusLabel.innerText = "";
                distanceLabel.innerText = "Total Distance: " + distanceKm + " KM";
                
                syncHiddenFields(distanceKm, mode, JSON.stringify(waypoints));

            } catch (error) {
                statusLabel.innerText = "Error calculating route.";
                distanceLabel.innerText = "";
            }
        }

        function syncHiddenFields(distance, mode, waypointsJson) {
            document.getElementById('<%= hfTotalDistance.ClientID %>').value = distance;
            document.getElementById('<%= hfTravelMode.ClientID %>').value = mode;
            document.getElementById('<%= hfWaypointsJSON.ClientID %>').value = waypointsJson;
            
            if(waypoints.length >= 2) {
                document.getElementById('<%= hfOriginName.ClientID %>').value = waypoints[0].name;
                document.getElementById('<%= hfDestinationName.ClientID %>').value = waypoints[waypoints.length - 1].name;
            } else {
                document.getElementById('<%= hfOriginName.ClientID %>').value = "";
                document.getElementById('<%= hfDestinationName.ClientID %>').value = "";
            }
        }

        function ValidateRouteData() {
            var dist = document.getElementById('<%= hfTotalDistance.ClientID %>').value;
            if(!dist || dist === "") {
                alert("Please build a complete route on the map before submitting.");
                return false;
            }
            return true;
        }

        function drawActualRoute(geoJsonData) {
            routeLayer = L.geoJSON(geoJsonData, { style: { color: '#1ABB9C', weight: 6, opacity: 0.8 } }).addTo(map);
            
            // Draw numbered pins for the route
            waypoints.forEach((wp, index) => {
                var marker = L.marker([wp.lat, wp.lon], {icon: getNumberedIcon(index + 1)}).bindPopup(`<b>${index + 1}.</b> ${wp.name}`).addTo(map);
                currentRoutePins.push(marker);
            });
            
            map.fitBounds(routeLayer.getBounds(), { padding: [40, 40] });
        }

        function clearSearchPins() { searchPins.forEach(m => map.removeLayer(m)); searchPins = []; }
        function clearRouteGraphics() {
            if (routeLayer) map.removeLayer(routeLayer);
            currentRoutePins.forEach(m => map.removeLayer(m));
            currentRoutePins = [];
        }

        map.on('click', function(e) {
            var lat = e.latlng.lat.toFixed(5);
            var lon = e.latlng.lng.toFixed(5);
            
            var popupDiv = document.createElement('div');
            popupDiv.innerHTML = `<b>Selected Map Point</b><br/><small>${lat}, ${lon}</small><br/>`;
            
            var btn = document.createElement('button');
            btn.className = "btn btn-xs btn-success mt-2";
            btn.innerText = "Add Stop Here";
            btn.type = "button";
            
            btn.onclick = function() {
                addWaypoint(lat, lon, `Map Point (${lat}, ${lon})`);
                map.closePopup();
            };
            
            popupDiv.appendChild(btn);
            
            L.popup().setLatLng(e.latlng).setContent(popupDiv).openOn(map);
        });

        // --- LOAD SAVED ROUTE LOGIC ---
        function loadSavedRoute(btnElement) {
            // 1. Retrieve the saved data from the button's HTML attributes
            var savedWaypointsJson = btnElement.getAttribute('data-waypoints');
            var savedMode = btnElement.getAttribute('data-mode');

            try {
                // 2. Parse the JSON back into our live array
                waypoints = JSON.parse(savedWaypointsJson);
                
                // 3. Update the dropdown UI
                document.getElementById('ddlMode').value = savedMode;

                // 4. Update the visual list and calculate the route line
                updateWaypointListUI();
                calculateAndDrawRoute();

                // 5. Smoothly scroll the user back to the top to see the map
                window.scrollTo({ top: 0, behavior: 'smooth' });
                
                document.getElementById('lblStatus').innerText = "Loaded saved route successfully.";
                document.getElementById('lblStatus').style.color = "#1ABB9C";

            } catch (err) {
                alert("Error loading saved route data.");
                console.error(err);
            }
        }
    </script>
</asp:Content>
