# InfluenceMap
 Attempt to make an influence map in Unity like the one in Endless space.
 
 *This project is a proof of concept. A small personal challenge. The code is delivered as is and no big optimization has been done on it.*

![Kpxi7sArxr](https://user-images.githubusercontent.com/23404599/124132915-99266a00-da81-11eb-8a5c-3a5195fd11fa.gif)


## How it works

I make multiple passes between two textures to achieve this result.
* In the first pass I render all the discs with negative values as an SDF using the [smin function](https://www.iquilezles.org/www/articles/smin/smin.htm) of Inigo Quilez.

![Unity_aHrTsTaNJe](https://user-images.githubusercontent.com/23404599/124138942-6ed7ab00-da87-11eb-9d00-06b73e4d69c2.png)

*The values being in negative, the preview above has been reversed for a more comprehensive rendering.*

* I take only the lowest channels (and therefore the ones closest to the center of their discs) and output a flat value.
If green is the channel with the lowest value, I get pure green (0.0, 1.0, 0.0, 0.0).
This gives me clean blobs where the zones do not overlap.

![Unity_jzaNsi3uZl](https://user-images.githubusercontent.com/23404599/124138275-c9243c00-da86-11eb-9c10-c7f80a068627.png)

*The black area is the alpha channel. It is not visible but is also used.*


* I blur to get nice even gradients. The gradient is done in two passes (vertical then horizontal for preformance reasons)

![Unity_QTCxuzKiiS](https://user-images.githubusercontent.com/23404599/124138446-f1139f80-da86-11eb-8b68-6469bc70803f.png)



* Then it is very easy in the last pass to reverse the gradients and isolate nice edges too.

![Unity_iDYWt0fMDG](https://user-images.githubusercontent.com/23404599/124138332-d6d9c180-da86-11eb-9683-94f33e723710.png)

*Here I use custom colors by channels, so I could put white instead of the alpha channel.*


# Second version

I made a second version in an attempt not to be limited to 4 colors.
I also replaced the blur by a jumpFlooding, in order to have a fast result for the gradients and more exact borders between the blobs.

This version relies much more on a compute shader & compute buffer. It can potentially become very heavy quickly.
In the other hand, it should be possible to easily compute it on a small buffer and resized afterwards only for the last step.

https://user-images.githubusercontent.com/23404599/161350056-9a435677-5451-490f-a72a-3863a6f5668f.mp4

![vlc_WcpmQUuGqQ](https://user-images.githubusercontent.com/23404599/161350030-3aba141e-36ff-4314-a908-5f30466150d1.gif)

