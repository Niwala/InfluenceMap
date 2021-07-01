# InfluenceMap
 Attempt to make an influence map like the one in Endless space.

![Kpxi7sArxr](https://user-images.githubusercontent.com/23404599/124132915-99266a00-da81-11eb-8a5c-3a5195fd11fa.gif)


## How it works

I make multiple passes between two textures to achieve this result.
⋅⋅*In the first pass I render all the discs with negative values as an SDF using the [smin function](https://www.iquilezles.org/www/articles/smin/smin.htm) of Inigo Quilez.

⋅⋅*Then I take only the lowest channels (and therefore the ones closest to the center of their discs) and output a flat value.
If green is the channel with the lowest value, I get pure green (0.0, 1.0, 0.0, 0.0).
This gives me clean blobs where the zones do not overlap.
![Unity_jzaNsi3uZl](https://user-images.githubusercontent.com/23404599/124137430-f7ede280-da85-11eb-8fe7-428c24ab92b4.png)
*The black area is the alpha channel. It is not visible but is also used.*

⋅⋅*I then blur to get nice even gradients. The gradient is done in two passes (vertical then horizontal for preformance reasons)
![Unity_QTCxuzKiiS](https://user-images.githubusercontent.com/23404599/124137717-400d0500-da86-11eb-99f0-00fba264719d.png)

⋅⋅*Then it is very easy in the last pass to reverse the gradients and isolate nice edges too.
![Unity_iDYWt0fMDG](https://user-images.githubusercontent.com/23404599/124137936-777bb180-da86-11eb-96d5-0436e883741b.png)
*Here I use custom colors by channels, so I could put white instead of the alpha channel.*
