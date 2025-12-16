import mapboxgl from 'mapbox-gl'
import { useEffect, useRef, useState } from 'react';
import 'mapbox-gl/dist/mapbox-gl.css';
import './map.css';

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
    latitude: number;
    longitude: number;
}

function Map({ route, coordinates }: { route: Route, coordinates: any[] }) {
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

        var routeToDraw = route.legs.flat().map(x => x.waypoints).flat().map(x => [x.longitude, x.latitude]);
        mapRef.current.getSource('route').setData({
            'type': 'Feature',
            'properties': {},
            'geometry': {
                'type': 'LineString',
                'coordinates': routeToDraw
            }
        });
    }, [route]);

    useEffect(() => {
        if (!mapRef.current || !coordinates)
            return;

        for (const [index, coordinate] of coordinates.entries()) {
            let myLatlng = new mapboxgl.LngLat(coordinate.lng, coordinate.lat);
            let marker = new mapboxgl.Marker()
                .setLngLat(myLatlng)
                .setPopup(new mapboxgl.Popup({ offset: 25 }).setText((index + 1).toString()))
                .addTo(mapRef.current);
        }

        if (route && route.legs[0]) {
            const firstWaypoint = route.legs[0]?.waypoints?.[0];
            const lastLeg = route.legs[route.legs.length - 1];
            const lastWaypoint = lastLeg?.waypoints?.[lastLeg.waypoints.length - 1];

            mapRef.current.fitBounds([
                [firstWaypoint.longitude, firstWaypoint.latitude],
                [lastWaypoint.longitude, lastWaypoint.latitude]
            ], {
                padding: 50,
                maxZoom: 15,
                duration: 1000
            });

        }

    }, [coordinates, route]);

    return (
        <div id='map-container' ref={mapContainerRef} />
    );
}

export default Map;