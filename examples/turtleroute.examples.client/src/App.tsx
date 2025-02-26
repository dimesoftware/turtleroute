import './App.css';
import Geocode from './components/pages/geocode';
import Route from './components/pages/route';

function App() {
    return (
        <div>
            <h1 id="tableLabel">TurtleRoute</h1>

            <Geocode />
            <Route />
        </div>
    );
}

export default App;