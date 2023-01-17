export function Score({ total = 0, position, player }) {
  return (
    <div className={position}>
      <h2>Player {player}</h2>
      <h2>{total}</h2>
    </div>
  );
}
