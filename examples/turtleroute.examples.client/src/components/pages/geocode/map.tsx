import mapboxgl from 'mapbox-gl'
import { useEffect, useRef } from 'react';
import 'mapbox-gl/dist/mapbox-gl.css';
import './map.css';

interface Coordinate {
    lat: number;
    lng: number;
}

function Map({ coordinates }: { coordinates: Coordinate }) {
    const mapRef = useRef<any>(null);
    const mapContainerRef = useRef<any>(null);
    const markerRef = useRef<any>(null);

    useEffect(() => {
        mapboxgl.accessToken = (import.meta as any).env.VITE_MAPBOX_ACCESS_TOKEN;
        mapRef.current = new mapboxgl.Map({
            container: mapContainerRef.current,
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [-74.5, 40],
            zoom: 9,
        });

        mapRef.current.addControl(new mapboxgl.NavigationControl());
    }, [])

    useEffect(() => {
        if (!mapRef.current || !coordinates) return;

        if (!markerRef.current) {
            // Create a new marker if it doesn't exist
            mapRef.current.setCenter([coordinates.lng, coordinates.lat]);
            markerRef.current = new mapboxgl.Marker().setLngLat([coordinates.lng, coordinates.lat]).addTo(mapRef.current);
        } else {
            // Update marker position if it already exists
            markerRef.current.setLngLat([coordinates.lng, coordinates.lat]);
        }

        console.log("Marker updated:", coordinates);
    }, [coordinates]);

    return (
        <div id='map-container' ref={mapContainerRef} />
    );
}

export default Map;