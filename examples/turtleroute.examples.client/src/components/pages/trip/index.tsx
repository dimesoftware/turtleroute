import { useState } from 'react';
import './map.css';
import Map from './map'
import { useForm, SubmitHandler, useFieldArray } from "react-hook-form"

interface Trip {
    distance: number;
    duration: number;
    routes: Route[];
}

interface Route {
    waypoints: Coordinate[]
}

interface Coordinate {
    lat: number;
    lng: number;
    distance: number;
    duration: number;
}

type Inputs = {
    from: string,
    to: string
}

function App() {
    const [trip, setTrip] = useState<Trip>();
    const [route, setRoute] = useState<Route>();
    const [coordinates, setCoordinates] = useState<Coordinate[]>();
    const { register, control, handleSubmit, watch, formState: { errors }, } = useForm<any>()
    const { fields, append, prepend, remove, swap, move, insert } = useFieldArray({ control, name: "stops", });

    const onSubmit: SubmitHandler<Inputs> = async (data) => {
        console.log("Submitting request...");

        const stops = (data as any).stops;
        var body = JSON.stringify(stops);
        const response = await fetch('/trip', {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: body
        });

        if (response.ok) {
            const trip = await response.json();
            setTrip(trip);
            setRoute({ waypoints: trip.routes.map((x: any) => x.waypoints).flat() });
            setCoordinates(trip.routes.map((x: any) => x.waypoints[0]).flat());
        } else {
            console.log("Oops!");
        }
    }

    return (
        <div>
            <p>
                Total distance: {Math.floor((trip?.distance ?? 0) / 1000) ?? 0} km.
                <br />
                Total duration: {Math.floor((trip?.duration ?? 0) / 60) ?? 0} minutes ({Math.floor((trip?.duration ?? 0) / 3600) ?? 0} hours).
            </p>

            <center>
                <table>
                    <thead>
                        <tr>
                            <th>Stop</th>
                            <th>Distance (km)</th>
                            <th>Duration (min)</th>
                            <th>Duration (hours)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {(trip?.routes ?? []).map((x, i) => (
                            <tr key={i}>
                                <td>{i + 1}</td>
                                <td>{Math.floor((x?.distance ?? 0) / 1000) ?? 0}</td>
                                <td>{Math.floor((x?.duration ?? 0) / 60) ?? 0}</td>
                                <td>{Math.floor((x?.duration ?? 0) / 3600) ?? 0}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </center>
            <br />

            <form onSubmit={handleSubmit(onSubmit)}>
                {fields.map((field, index) => (
                    <>
                        <input key={field.id} {...register(`stops.${index}`)} style={{ marginBottom: "5px" }} />
                        <br />
                    </>
                ))
                }

                <button type="button" onClick={() => append("Empire State Building")}>
                    Add stop
                </button>

                <br />

                <input type="submit" />
            </form>

            <Map route={route} coordinates={coordinates} />
        </div >
    );
}

export default App;