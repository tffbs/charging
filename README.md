# Charging scheduler

This Web api responds with an EV charging schedule in a following manner:

### 1. Validation and calculation of early values

conversion to percentage, as user settings contains percentage, while car data has kWh

if we are on the desired state of charge already, return with a timespan all the way to leaving time with IsCharging -> False

### 2. Calculation of direct charge (if required)

Check if we need to direct charge. We need to direct charge if the battery charge is under the direct charge percentage

we need the kWh amount to know how long we need to charge
calculating time needed based on missing charge and charge power -> kWh / kW = h
converting hours in decimal to DateTime -> might be some loss of accuracy due to decimal to double casting
adjust starting time and current percentage
adding the time span to the response

calculating remaining charging time 

### 3. Handling single tariff

Checking if we only received one tariff for the whole charging duration, if we did return a charging timespan until desired charging state and a false if there's remaining time

### 3. Creating timespans for the entire duration

We go through the tariffs and create appropriate timespans, including the energy price for later

When we find a tariff that corresponds with the current time we create the timspan and advance the current date

When the current date and the leaving time is equal the loop ends.

### 4. Setting IsCharging boolean for timespans

Sort the tariff prices to efficiently select the cheapest one

Going from the lowest price, set the timespans to charging
As the remaining time gets shorter we go up in price
This way we'll find the cheapest total charge

Check if we are charging in the particular timespan and if the price is low

If there is still remaining charging time stay in the loop
If there isn't exit the loop

If we would exceed the desired capacity break up the span to charging and non-charging timespans

### 5. Merging consecutive charging and non-charging timespans


### 6. Return the schedule
