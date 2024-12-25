document.getElementById("monteCarloForm").addEventListener("submit", function (e) 
{
    e.preventDefault();

    // input
    const input = {
        stockPrice: parseFloat(document.getElementById("stockPrice").value),
        strikePrice: parseFloat(document.getElementById("strikePrice").value) || 0,
        riskFreeRate: parseFloat(document.getElementById("riskFreeRate").value),
        volatility: parseFloat(document.getElementById("volatility").value),
        timeToMaturity: parseFloat(document.getElementById("timeToMaturity").value),
        numberOfSteps: parseInt(document.getElementById("numberOfSteps").value, 10),
        numberOfSimulations: parseInt(document.getElementById("numberOfSimulations").value, 10),
        useAntithetic: document.getElementById("useAntithetic").checked,
        controlVariate: document.getElementById("controlVariate").checked,
        parallelExecution: document.getElementById("parallelExecution").checked,
        optionClass: document.getElementById("optionClass").value,
        barrierClass: document.getElementById("barrierClass").value || null,
        p: parseFloat(document.getElementById("p").value) || 0,
        h: parseFloat(document.getElementById("h").value) || 0,
        lookBackClass: document.getElementById("lookBackClass").value || null
    };


    const request = new XMLHttpRequest();

    request.open("POST", "http://localhost:5055/api/MonteCarlo/simulate", true);
    request.setRequestHeader("Content-Type", "application/json");

    request.onreadystatechange = function () 
    {
        if (request.readyState === XMLHttpRequest.DONE) 
        {
            if (request.status === 200) 
            {
                const data = JSON.parse(request.responseText);

                document.getElementById("callPrice").textContent = data.callPrice;
                document.getElementById("putPrice").textContent = data.putPrice;
                document.getElementById("callSE").textContent = data.callSE;
                document.getElementById("putSE").textContent = data.putSE;
                document.getElementById("callDelta").textContent = data.callDelta;
                document.getElementById("putDelta").textContent = data.putDelta;
                document.getElementById("callGamma").textContent = data.callGamma;
                document.getElementById("putGamma").textContent = data.putGamma;
                document.getElementById("callVega").textContent = data.callVega;
                document.getElementById("putVega").textContent = data.putVega;
                document.getElementById("callTheta").textContent = data.callTheta;
                document.getElementById("putTheta").textContent = data.putTheta;
                document.getElementById("callRho").textContent = data.callRho;
                document.getElementById("putRho").textContent = data.putRho;
            } 
            
            else 
            {
                console.error("Error:", request.statusText);
            }
        }
    };

    // send request
    request.send(JSON.stringify(input));
});


document.getElementById("optionClass").addEventListener("change", function ()
{
    const selectedOption = this.value;
    
    // if barrier option
    // display barrier option type and barrier level
    const barrierOptions = document.getElementById("barrierOptions");
    const h = document.getElementById("h");
    if (selectedOption === "B") 
    {
        barrierOptions.classList.remove("hidden");
        barrierOptions.classList.add("visible");
    
        h.classList.remove("hidden");
        h.classList.add("visible");
    } 
    else
    {
        barrierOptions.classList.remove("visible");
        barrierOptions.classList.add("hidden");
    
        h.classList.remove("visible");
        h.classList.add("hidden");
    }

    // if lookback option
    // display lookback option type
    const lookBackClass = document.getElementById("lookBackClass");
    if (selectedOption === "L") 
    {
        lookBackClass.classList.remove("hidden");
        lookBackClass.classList.add("visible");
    } 
    else 
    {
        lookBackClass.classList.remove("visible");
        lookBackClass.classList.add("hidden");
    }

    // if digital option
    // display payout
    const p = document.getElementById("p");
    if (selectedOption === "D") 
    {
        p.classList.remove("hidden");
        p.classList.add("visible");
    } 
    else 
    {
        p.classList.remove("visible");
        p.classList.add("hidden");
    }

    // if range option
    // hide control variate method and strike input
    const controlVariate = document.getElementById("controlVariate");
    const strikePrice = document.getElementById("strikePrice");
    if (selectedOption === "R") 
    {
        controlVariate.classList.remove("visible");
        controlVariate.classList.add("hidden");

        strikePrice.classList.remove("visible");
        strikePrice.classList.add("hidden");
    } 
    else 
    {
        controlVariate.classList.remove("hidden");
        controlVariate.classList.add("visible");
        
        strikePrice.classList.remove("hidden");
        strikePrice.classList.add("visible");
    }
});