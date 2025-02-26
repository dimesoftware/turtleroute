import { useState } from 'react';
import './map.css';
import Map from './map'
import { useForm, SubmitHandler } from "react-hook-form"

interface Coordinate {
    lat: number;
    lng: number;
}

type Inputs = {
    from: string,
    to: string
}

function App() {
    const [routes, setRoutes] = useState<Coordinate[]>();

    const { register, handleSubmit, watch, formState: { errors }, } = useForm<Inputs>()
    const onSubmit: SubmitHandler<Inputs> = async (data) => {
        console.log("Submitting request...");
        const response = await fetch('/route?from=' + data.from + "&to=" + data.to);
        if (response.ok) {
            const data = await response.json();

            console.log("Response from server:");
            console.log(data);
            setRoutes(data);
        } else {
            console.log("Oops!");
        }
    }

    return (
        <div>

            <h2 id="tableLabel">Route</h2>

            <form onSubmit={handleSubmit(onSubmit)}>
                {/* register your input into the hook by invoking the "register" function */}
                <label>From:</label>
                <input defaultValue="Central Park" {...register("from")} />

                <label>To:</label>
                <input defaultValue="Empire State Building" {...register("to")} />
                <input type="submit" />
            </form>

            <Map route={routes} />
        </div>
    );
}

export default App;