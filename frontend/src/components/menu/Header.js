export function Header({ onClick, showButton, level }) {
  return (
    <div className="Header">
      <div className="menu">
        <h1>Welcome to Pong</h1>
        <div className="btnWrapper">
          {showButton && (
            <button className="btn" onClick={onClick}>
              Play Game
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
