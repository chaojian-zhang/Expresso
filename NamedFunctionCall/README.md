# Named Function Call

This experiments tests the suitability of using fully qualified function name as a means to invoke native CLR (static) functions. 

As it turns out, for something as seemingly straightforward as `Regex` Class from `System.Text.RegularExpressions` namespace (and the dll), the `Regex.Replace` function has multiple signatures for suitable development use - but such ambiguity makes it hard to refer to things purely by name.

As it turns out, it makes more sense to provide a set of curated functions for Espresso (and maybe Parcel V6) purpose, instead of just exposing everything from CLR.