#include "QHBallPossessionComponent.h"

#include "QHBasketball.h"
#include "Components/SkeletalMeshComponent.h"
#include "GameFramework/Character.h"
#include "Net/UnrealNetwork.h"

UQHBallPossessionComponent::UQHBallPossessionComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
    SetIsReplicatedByDefault(true);
}

void UQHBallPossessionComponent::RequestPickup(AQHBasketball* Ball)
{
    if (Ball)
    {
        ServerPickup(Ball);
    }
}

void UQHBallPossessionComponent::ServerPickup_Implementation(AQHBasketball* Ball)
{
    ACharacter* Character = Cast<ACharacter>(GetOwner());
    if (!Character || !Ball || CurrentBall || !Ball->IsAvailable())
    {
        return;
    }

    if (FVector::DistSquared(Character->GetActorLocation(), Ball->GetActorLocation()) >
        FMath::Square(PickupRange))
    {
        return;
    }

    CurrentBall = Ball;
    State = EQHBallPossessionState::Holding;
    Ball->SetHeldState(true);
    AttachBall(Ball);
    GetOwner()->ForceNetUpdate();
}

void UQHBallPossessionComponent::RequestRelease(const FVector ReleaseVelocity)
{
    ServerRelease(ReleaseVelocity);
}

void UQHBallPossessionComponent::ServerRelease_Implementation(const FVector_NetQuantize ReleaseVelocity)
{
    if (CurrentBall)
    {
        ReleaseBall(FVector(ReleaseVelocity));
    }
}

void UQHBallPossessionComponent::AttachBall(AQHBasketball* Ball)
{
    ACharacter* Character = Cast<ACharacter>(GetOwner());
    if (!Character || !Ball)
    {
        return;
    }

    USkeletalMeshComponent* CharacterMesh = Character->GetMesh();
    if (CharacterMesh && CharacterMesh->DoesSocketExist(HandSocketName))
    {
        Ball->AttachToComponent(CharacterMesh, FAttachmentTransformRules::SnapToTargetNotIncludingScale, HandSocketName);
    }
    else
    {
        Ball->AttachToComponent(Character->GetRootComponent(), FAttachmentTransformRules::KeepWorldTransform);
        Ball->SetActorRelativeLocation(FVector(35.0f, 25.0f, 55.0f));
    }
}

void UQHBallPossessionComponent::ReleaseBall(const FVector& ReleaseVelocity)
{
    AQHBasketball* Ball = CurrentBall;
    if (!Ball)
    {
        State = EQHBallPossessionState::Empty;
        return;
    }

    Ball->DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
    Ball->SetHeldState(false);
    Ball->Collision->SetPhysicsLinearVelocity(ReleaseVelocity, false);
    CurrentBall = nullptr;
    State = EQHBallPossessionState::Empty;
    GetOwner()->ForceNetUpdate();
}

void UQHBallPossessionComponent::OnRep_CurrentBall()
{
    if (CurrentBall)
    {
        AttachBall(CurrentBall);
    }
}

void UQHBallPossessionComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(UQHBallPossessionComponent, CurrentBall);
    DOREPLIFETIME(UQHBallPossessionComponent, State);
}
