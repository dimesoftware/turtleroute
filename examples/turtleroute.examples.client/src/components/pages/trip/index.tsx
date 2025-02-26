import { useState } from 'react';
import './map.css';
import Map from './map'
import { useForm, SubmitHandler, useFieldArray } from "react-hook-form"

interface Trip {

}

interface Route {
    waypoints: Coordinate[]
}

interface Coordinate {
    lat: number;
    lng: number;
}

type Inputs = {
    from: string,
    to: string
}

function App() {
    const [trip, setTrip] = useState<Trip>();
    const [route, setRoute] = useState<Route>();
    const { register, control, handleSubmit, watch, formState: { errors }, } = useForm<any>()
    const { fields, append, prepend, remove, swap, move, insert } = useFieldArray({ control, name: "stops", });

    const onSubmit: SubmitHandler<Inputs> = async (data) => {
        console.log("Submitting request...");

        var body = JSON.stringify((data as any).stops.map((x: any) => ({ Latitude: x.lat, Longitude: x.lng })));
        const response = await fetch('/trip', {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: body
        });

        if (response.ok) {
            const trip = await response.json();

            console.log("Response from server:");
            console.log(trip);
            setTrip(trip);
            setRoute({ waypoints: trip.routes.map((x: any) => x.waypoints).flat() });

        } else {
            console.log("Oops!");
        }
    }

    const createRandomCoordinate = () => {
        const nycCoordinates = [
            { lat: '40.712776', lng: '-74.005974' }, // NYC (General)
            { lat: '40.730610', lng: '-73.935242' }, // East Village
            { lat: '40.758896', lng: '-73.985130' }, // Times Square
            { lat: '40.748817', lng: '-73.985428' }, // Empire State Building
            { lat: '40.706192', lng: '-74.008874' }, // Wall Street
            { lat: '40.730824', lng: '-73.997330' }, // Washington Square Park
            { lat: '40.782865', lng: '-73.965355' }, // Central Park
            { lat: '40.689247', lng: '-74.044502' }, // Statue of Liberty
            { lat: '40.752726', lng: '-73.977229' }, // Grand Central Terminal
            { lat: '40.750504', lng: '-73.993439' }, // Madison Square Garden
        ];

        return nycCoordinates[Math.floor(Math.random() * nycCoordinates.length)];
    }

    return (
        <div>

            <h2 id="tableLabel">Trip</h2>

            <form onSubmit={handleSubmit(onSubmit)}>
                {fields.map((field, index) => (
                    <>
                        <label>Lat:</label>
                        <input key={field.id} {...register(`stops.${index}.lat`)} />

                        <label>Lng:</label>
                        <input key={field.id} {...register(`stops.${index}.lng`)} />

                        <br />
                    </>
                ))
                }

                <button type="button" onClick={() => append(createRandomCoordinate())}>
                    Add
                </button>

                <br />

                <input type="submit" />
            </form>

            <Map route={route} />
        </div>
    );
}

export default App;