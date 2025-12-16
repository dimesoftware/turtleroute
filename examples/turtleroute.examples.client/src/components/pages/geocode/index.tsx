import { useEffect, useState } from 'react';
import Map from './map'
import { useForm, SubmitHandler } from "react-hook-form"

interface Coordinate {
    lat: number;
    lng: number;
}

type Inputs = {
    query: string
}

function App() {
    const [coordinates, setCoordinates] = useState<Coordinate>();

    const { register, handleSubmit, watch, formState: { errors }, } = useForm<Inputs>()
    const onSubmit: SubmitHandler<Inputs> = async (data) => {
        console.log("Submitting request...");
        const response = await fetch('/coordinates?search=' + data.query);
        if (response.ok) {
            const data = await response.json();

            console.log("Response from server:");
            console.log(data);
            setCoordinates({ lat: data.latitude, lng: data.longitude });
        } else {
            console.log("Oops!");
        }
    }

    return (
        <div>
            <h2 id="tableLabel">Geocode</h2>

            <form onSubmit={handleSubmit(onSubmit)}>
                {/* register your input into the hook by invoking the "register" function */}
                <input defaultValue="Empire State Building" {...register("query")} />
                <input type="submit" />
            </form>

            <p>Coordinates:</p>
            <p> Lat: {coordinates?.lat} <br />
                Lng: {coordinates?.lng} </p>

            <Map coordinates={coordinates} />

        </div>
    );
}

export default App;