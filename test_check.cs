// The activeSegments array refresh we put in earlier remains perfect.
// When the callback fires FinalizeDestruction, it calls OnSegmentDestroyed, which removes from activeSegments, then pushes that cleanly to the SpacingManager.
