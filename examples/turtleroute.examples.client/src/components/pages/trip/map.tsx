import mapboxgl from 'mapbox-gl'
import { useEffect, useRef, useState } from 'react';
import 'mapbox-gl/dist/mapbox-gl.css';
import './map.css';

interface Route {
    waypoints: Coordinate[]
}
interface Coordinate {
    latitude: number;
    longitude: number;
}

function Map({ route }: { route: Route }) {
    if (route)
        console.log(route);

    const mapRef = useRef<any>(null);
    const mapContainerRef = useRef<any>(null);
    const routeRef = useRef<any>(null);

    useEffect(() => {
        mapboxgl.accessToken = (import.meta as any).env.VITE_MAPBOX_ACCESS_TOKEN;
        mapRef.current = new mapboxgl.Map({
            container: mapContainerRef.current,
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [-74.5, 40],
            zoom: 9,
        });

        mapRef.current.addControl(new mapboxgl.NavigationControl());

        mapRef.current.on('load', () => {
            console.log("Crikey");

            if (!mapRef.current.getSource('route')) {
                mapRef.current.addSource('route', {
                    'type': 'geojson',
                    'data': {
                        'type': 'Feature',
                        'properties': {},
                        'geometry': {
                            'type': 'LineString',
                            'coordinates': []
                        }
                    }
                });
            }

            if (!mapRef.current.getLayer('route')) {

                mapRef.current.addLayer({
                    'id': 'route',
                    'type': 'line',
                    'source': 'route',
                    'layout': {
                        'line-join': 'round',
                        'line-cap': 'round'
                    },
                    'paint': {
                        'line-color': 'green',
                        'line-width': 5
                    }
                });
            }

        }, { once: true });
    }, [])


    useEffect(() => {
        if (!mapRef.current || !route)
            return;

        var routeToDraw = (route?.waypoints ?? []).map(x => [x.longitude, x.latitude]);
        mapRef.current.getSource('route').setData({
            'type': 'Feature',
            'properties': {},
            'geometry': {
                'type': 'LineString',
                'coordinates': routeToDraw
            }
        });
    }, [route]);

    return (
        <div id='map-container' ref={mapContainerRef} />
    );
}

export default Map;