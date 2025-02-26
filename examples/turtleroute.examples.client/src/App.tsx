import './App.css';
import Geocode from './components/pages/geocode';
import Trip from './components/pages/trip';

function App() {
    return (
        <div>
            <h1 id="tableLabel">TurtleRoute</h1>

            <Geocode />
            <Trip />
        </div>
    );
}

export default App;