export const betweenPoints = (y, top, bottom) => {
  if (y < top || y > bottom) {
    return false;
  }
  return true;
};
