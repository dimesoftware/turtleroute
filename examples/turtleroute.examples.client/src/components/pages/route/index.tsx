import { useState } from 'react';
import './map.css';
import Map from './map'
import { useForm, SubmitHandler, useFieldArray } from "react-hook-form"

interface Route {
    distance: number;
    duration: number;
    legs: Leg[];
}

interface Leg {
    distance: number;
    duration: number;
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
            const route = await response.json();
            setRoute(route);
        } else {
            console.log("Oops!");
        }
    }

    const generateSample = async () => {
        const coordinates = [
            { lat: 53.3813861, lng: -2.367687, distance: 0, duration: 0 },
            { lat: 51.86455154418945, lng: -2.246934175491333, distance: 0, duration: 0 },
            { lat: 52.479286193847656, lng: -1.9029406309127808, distance: 0, duration: 0 },
            { lat: 51.209179, lng: -0.5676603, distance: 0, duration: 0 },
        ];

        setCoordinates(coordinates);

        var body = JSON.stringify(coordinates);
        const response = await fetch('/sample', {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: body
        });

        if (response.ok) {
            const route = await response.json();
            setRoute(route);
        } else {
            console.log("Oops!");
        }
    }

    return (
        <div>
            <p>
                Total distance: {Math.floor((route?.distance ?? 0) / 1000) ?? 0} km.
                <br />
                Total duration: {Math.floor((route?.duration ?? 0) / 60) ?? 0} minutes ({Math.floor((route?.duration ?? 0) / 3600) ?? 0} hours).
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
                        {(route?.legs ?? []).map((x, i) => (
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

            <div style={{ marginTop: "10px" }}>
                <button onClick={() => generateSample()}>Generate sample</button>
            </div>
        </div>
    );
}

export default App;